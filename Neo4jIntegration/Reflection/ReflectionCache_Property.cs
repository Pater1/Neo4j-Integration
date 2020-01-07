using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Neo4jIntegration.Reflection
{
    public partial class ReflectionCache
    {
        public class Property
        {
            public static object[] fastEmpty = new object[0];
            public bool isID;
            public readonly PropertyInfo info;
            public readonly INeo4jAttribute[] neo4JAttributes;
            public readonly ICustomDBSchema customDBSchema;
            public bool WrittenTo { get; internal set; }

            public string Name => info.Name;
            public IEnumerable<IOnReadAttribute> onReads => neo4JAttributes.Where(x => x is IOnReadAttribute).Cast<IOnReadAttribute>();
            public IEnumerable<IOnWriteAttribute> onWrites => neo4JAttributes.Where(x => x is IOnWriteAttribute).Cast<IOnWriteAttribute>();
            
            public bool IsCollection => typeof(IEnumerable).IsAssignableFrom(info.PropertyType) && !typeof(NoDBCollection).IsAssignableFrom(info.PropertyType) && info.PropertyType != typeof(string);

            public Property(PropertyInfo buildFrom, bool noCheck)
            {
                info = buildFrom;
                neo4JAttributes = buildFrom.GetCustomAttributes(true)
                    .Where(x => x is INeo4jAttribute)
                    .Cast<INeo4jAttribute>()
                    .ToArray();
                WrittenTo = false;
                try
                {
                    customDBSchema = neo4JAttributes.Where(a => typeof(ICustomDBSchema).IsAssignableFrom(a.GetType())).SingleOrDefault() as ICustomDBSchema;
                    if (customDBSchema == null)
                    {
                        customDBSchema = new StoreDirectAttribute();
                    }
                }
                catch
                {
                    if (!noCheck)
                    {
                        throw new ArgumentException("A column in the database may only declare one ICustomDBSchema");
                    }
                }
            }

            public ReadQueryParams<T> ReadValue<T>(ReadQueryParams<T> readQueryParams)
            {
                if (typeof(ICustomDBSchema<>).TryMakeGenericType(out System.Type t, info.PropertyType) && t.IsAssignableFrom(info.PropertyType))
                {
                    object tInst = Activator.CreateInstance(info.PropertyType);
                    Expression cont = Expression.Constant(tInst, t);
                    ParameterExpression rqp = Expression.Parameter(typeof(ReadQueryParams<T>), "readQueryParams");
                    MethodInfo rv = t.GetMethod(nameof(ReadValue)).MakeGenericMethod(typeof(T));
                    LambdaExpression lambdaExpression = Expression.Lambda(
                        Expression.Call(
                            cont,
                            rv,
                            rqp
                        ),
                        rqp
                    );
                    Delegate comp = lambdaExpression.Compile();

                    return (ReadQueryParams<T>)comp.DynamicInvoke(readQueryParams);
                }
                else
                {
                    return customDBSchema.ReadValue(readQueryParams);
                }
            }

            public object PullValue<T>(T instance)
            {
                var v = info.GetValue(instance, fastEmpty);
                return v;
            }
            public void PushValue<T>(T backingInstance, object value)
            {
                info.SetValue(backingInstance, value);
                WrittenTo = true;
            }

            public object WriteValidate<T>(DependencyInjector depInj, T bi)
            {
                depInj.Insert("value", PullValue(bi));
                foreach (var v in onWrites)
                {
                    if (v.OnWrite(depInj))
                    {
                        WrittenTo = true;
                    }
                }
                //TODO: remove magic strings
                if (Name == "Id")
                {
                    PushValue(bi, depInj.Get("value"));
                    string id = depInj.Get("value").ToString();
                    if(id.StartsWith("\"") && id.EndsWith("\""))
                    {
                        PushValue(bi, new string(id.Skip(1).Reverse().Skip(1).Reverse().ToArray()));
                        WrittenTo = true;
                        return id;
                    }
                    else
                    {
                        return "\"" + id + "\"";
                    }
                }
                return depInj.Get("value");
            }
            private Property(Property buildFrom)
            {
                info = buildFrom.info;
                neo4JAttributes = buildFrom.neo4JAttributes; //clone
                WrittenTo = false;
                customDBSchema = buildFrom.customDBSchema;
            }
            public Property DeepClone()
            {
                return new Property(this);
            }
        }
    }
}
