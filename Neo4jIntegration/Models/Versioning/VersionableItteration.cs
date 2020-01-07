using Newtonsoft.Json;
using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Neo4jIntegration.Models.Versioning
{

    [System.Serializable]
    public class VersionableItteration<T> : INeo4jNode, ICustomDBSchema<VersionableItteration<T>>
    {
        [ID(ID.IDType.String, ID.CollisionResolutionStrategy.Rand_Base62_10)]
        public string Id { get; private set; }
        public bool IsActive { get; set; } = true;

        //[ReferenceThroughRelationship("PREVIOUS")]
        [DBIgnore]
        public VersionableItteration<T> Previous { get; set; }

        //[ReferenceThroughRelationship("NEXT")]
        [DBIgnore]
        public VersionableItteration<T> Next { get; set; }

        [JsonEncode(typeof(DateTime))]
        public DateTime ItteratedTime { get; set; }

        //have default DB save/load ignore this value so we can handle it manually (for now)
        [DBIgnore]
        public T Value { get; set; }

        [JsonEncode(typeof(AcceptanceState))]
        public AcceptanceState AcceptanceState { get; set; }

        public VersionableItteration(T value, AcceptanceState acceptanceState)
        {
            Value = value;
            AcceptanceState = acceptanceState;
            ItteratedTime = DateTime.UtcNow;
        }
        public VersionableItteration() { }

        public static implicit operator T(VersionableItteration<T> vit)
        {
            return vit.Value;
        }

        private static readonly JsonSerializerSettings inlineSerializer = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            FloatFormatHandling = FloatFormatHandling.DefaultValue,
            NullValueHandling = NullValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Auto,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        };
        public SaveQuearyParams<VersionableItteration<T>> SaveValue(SaveQuearyParams<VersionableItteration<T>> qParams)
        {
            if(Value == null)
            {
                qParams.success = false;
                return qParams;
            }
            var undoParams = qParams;
            undoParams.success = false;
            qParams.success = true;

            qParams = qParams.Save();

            Type t = typeof(T);
            bool isCollection = t != typeof(string) && t.GetInterface(nameof(IEnumerable)) != null;
            Type nt = isCollection ? t.GetGenericArguments()[0] : t;
            bool isNode = typeof(INeo4jNode).IsAssignableFrom(nt);
            //TODO: link enums by reference
            if (!isNode)
            {
                //store all inline
                string valuesAsJson = "'" + JsonConvert.SerializeObject(Value, Formatting.None, inlineSerializer) + "'";
                qParams = DBOps<VersionableItteration<T>>.Writes<VersionableItteration<T>>.WriteSingleInline(qParams, "InlineValue", valuesAsJson);
            }
            else if (!isCollection)
            {
                //relationship to single
                INeo4jNode valRaw = (INeo4jNode)qParams.buildFor.Get(x => x.Value);
                if (valRaw != null)
                {
                    var vParams = qParams.ChainSaveNode(valRaw);
                    qParams = DBOps<VersionableItteration<T>>.Writes<VersionableItteration<T>>.WriteRelationship(qParams, qParams.objName, vParams.objName, "VALUE");
                }
                return undoParams;
            }
            else
            {
                //relationship to many
                Type asRefcollection = typeof(ReferenceCollection<>).MakeGenericType(nt);

                ParameterExpression param = Expression.Parameter(t, "sourceCollection");

                Expression cast;
                if (asRefcollection.IsAssignableFrom(Value.GetType()))
                {
                    cast = Expression.TypeAs(param, asRefcollection);
                }
                else
                {
                    ConstructorInfo cotr =
                        asRefcollection
                        .GetConstructors()
                        .Select(x => (x, x.GetParameters()))
                        .Where(x => x.Item2.Length == 1 && x.Item2.Select(a => a.ParameterType).First() == typeof(ICollection<>).MakeGenericType(nt))
                        .Single().x;

                    cast = Expression.New(
                        cotr, param
                    );
                }

                LambdaExpression lambda = Expression.Lambda(cast, param);
                Delegate del = lambda.Compile();

                INeo4jNode collectionToPersist = (INeo4jNode)del.DynamicInvoke(Value);
                qParams = qParams.ChainSaveNode(collectionToPersist);
            }

            return qParams;
        }

        public ReadQueryParams<A> ReadValue<A>(ReadQueryParams<A> rParams)
        {
            throw new NotImplementedException();
        }
    }
}
