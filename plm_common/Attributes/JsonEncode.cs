using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Newtonsoft.Json;

namespace plm_common.Attributes
{
    public class JsonEncode : Attribute, INeo4jAttribute, IOnWriteAttribute, IOnReadAttribute
    {
        private static Dictionary<Type, (Delegate serialze, Delegate deserialize)> nameCache
            = new Dictionary<Type, (Delegate serialze, Delegate deserialize)>();
        (Delegate serialze, Delegate deserialize) serializers;
        public JsonEncode(Type encodedType)
        {
            if (nameCache.ContainsKey(encodedType))
            {
                serializers = nameCache[encodedType];
            }
            else
            {
                Type jsonConvert = typeof(JsonConvert);

                ParameterExpression strParam = Expression.Parameter(typeof(string));
                ParameterExpression tParam = Expression.Parameter(encodedType);

                serializers.serialze = Expression.Lambda(
                        Expression.Call(
                            null,
                            jsonConvert.GetMethod(nameof(JsonConvert.SerializeObject), new Type[] { typeof(object) }),
                            Expression.Convert(tParam, typeof(object))
                        ),
                        tParam
                    ).Compile();

                serializers.deserialize = Expression.Lambda(
                        Expression.Call(
                            null,
                            jsonConvert.GetMethod(nameof(JsonConvert.DeserializeObject), 1, new Type[] { typeof(string) }).MakeGenericMethod(encodedType),
                            strParam
                        ),
                        strParam
                    ).Compile();

                nameCache.Add(encodedType, serializers);
            }
        }

        public void OnRead(DependencyInjector depInj)
        {
            string o = depInj.Get("value").ToString();
            depInj.Set("value", serializers.deserialize.DynamicInvoke(o));
        }

        public bool OnWrite(DependencyInjector depInj)
        {
            object o = depInj.Get("value");
            depInj.Set("value", serializers.serialze.DynamicInvoke(o));
            return false;
        }
    }
}
