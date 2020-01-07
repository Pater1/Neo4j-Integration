using Newtonsoft.Json;
using plm_common.Models;
using plm_common.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using RelDir = plm_common.DB.RelationshipDirection;

namespace plm_common.DB
{
    public static class DBOps<A>//<T> //where T: class, INeo4jNode, new()
    {
        public static class Reads
        {
            internal static ReadQueryParams<T> ReadRelationship<T>(ReadQueryParams<T> rParams, string relationship, RelDir relationshipDirection = RelDir.Forward)// where T : class, INeo4jNode, new()
            {
                string forward = relationshipDirection == RelDir.Forward ? ">" : "";
                string reverse = relationshipDirection == RelDir.Reverse ? "<" : "";

                string queary = $"({rParams.parentName}){reverse}-[:{relationship}]-{forward}({rParams.childName}:{rParams.prop.info.PropertyType.QuerySaveLabels()})";
                rParams.typeWrappedCypherFluentQuery.internalQ = rParams.typeWrappedCypherFluentQuery.internalQ.OptionalMatch(queary);

                rParams.AddIncludedChildNode();

                return rParams;
            }
        }
        public static class Writes<T> where T: class, INeo4jNode, new()
        {
            public static SaveQuearyParams<T> WriteSingleInline<U>(SaveQuearyParams<T> qParams, string propName, U value)
            {
                string paramName = qParams.objName + "." + propName;
                string paramNameSafe = qParams.objName + "_" + propName;

                if (value != null && (qParams.prop.WrittenTo || qParams.isNew) && !qParams.queryParams.ContainsKey(paramName))
                {
                    if (qParams.mutex != null)
                    {
                        lock (qParams.mutex)
                        {
                            qParams.queryParams.Add(paramName, value);
                            qParams.queryParams.Add(paramNameSafe, value);
                            qParams.queary = qParams.queary.Set($"{paramName} = {value}");
                        }
                    }
                    else
                    {
                        qParams.queryParams.Add(paramName, value);
                        qParams.queryParams.Add(paramNameSafe, value);
                        qParams.queary = qParams.queary.Set($"{paramName} = {value}");
                    }
                }

                return qParams;
            }

            public static SaveQuearyParams<T> WriteRelationship(
                SaveQuearyParams<T> qParams, 
                string objName1, 
                string objName2, 
                string relationshipLabel, 
                RelDir relationshipDirection = RelDir.Forward)
            {
                if(relationshipLabel == null)
                {
                    return qParams;
                }

                string forward = relationshipDirection == RelDir.Forward ? ">" : "";
                string reverse = relationshipDirection == RelDir.Reverse ? "<" : "";

                string queary = $"({objName1}){reverse}-[:{relationshipLabel}]-{forward}({objName2})";
                if (qParams.mutex != null)
                {
                    lock (qParams.mutex)
                    {
                        qParams.queary = qParams.queary.CreateUnique(queary);
                    }
                }
                else
                {
                    qParams.queary = qParams.queary.CreateUnique(queary);
                }

                return qParams;
            }
        }
    }
}
