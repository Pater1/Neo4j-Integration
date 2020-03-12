using Newtonsoft.Json;
using Neo4jIntegration.Models;
using Neo4jIntegration.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Neo4jIntegration.Reflection;
using Neo4jClient.Transactions;
using System.Linq;
using Neo4jIntegration.Attributes;
using static Neo4jIntegration.Reflection.ReflectionCache;
using Neo4jClient.Cypher;
using System.Collections;
using System.Linq.Expressions;

namespace Neo4jIntegration.DB
{
    public static class DBOps
    {
        public static void SaveNode<T>(ReflectReadDictionary<T> toSave, ITransactionalGraphClient client) where T : INeo4jNode
        {
            Dictionary<string, (string, bool)> savedIds = new Dictionary<string, (string, bool)>();
            Neo4jClient.Cypher.ICypherFluentQuery v = client.Cypher;

            var parms = SaveNodeInline(toSave, (v, savedIds));
            parms.query.ExecuteWithoutResults();
        }


        private static (ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) __SaveEnumerableNodeRelationship((object o, System.Type t) p1, (object o, System.Type t) p2, string relationshipName, (ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2)
        {
            if (p1.o == null || p2.o == null)
            {
                return parms;
            }

            MethodInfo meinf = typeof(DBOps)
                .GetMethod(nameof(DBOps._SaveEnumerableNodeRelationship), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(p1.t);
            ConstructorInfo contr = typeof(ReflectReadDictionary<>).MakeGenericType(p1.t).GetConstructors().Where(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == p1.t).Single();
            //TODO: paramaterize & cache
            LambdaExpression l = Expression.Lambda(
                Expression.Call(
                    null,
                    meinf,
                    Expression.New(contr, Expression.Convert(Expression.Constant(p1.o), p1.t)),
                    Expression.Constant(p2),
                    Expression.Constant(relationshipName),
                    Expression.Constant(parms),
                    Expression.Constant(forceNodeName1),
                    Expression.Constant(forceNodeName2)
                )
            );
            return ((ICypherFluentQuery query, Dictionary<string, (string, bool)> ids))l.Compile().DynamicInvoke();
        }

        private static (ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) _SaveEnumerableNodeRelationship<T>(ReflectReadDictionary<T> toSave, (object o, System.Type t) p, string relationshipName, (ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2) where T : INeo4jNode
        {
            if (p.o == null)
            {
                return parms;
            }

            MethodInfo meinf = typeof(DBOps)
                .GetMethod(nameof(DBOps.SaveEnumerableNodeRelationship), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(typeof(T), p.t);
            System.Type rrdt = typeof(ReflectReadDictionary<>).MakeGenericType(p.t);
            ConstructorInfo contr = rrdt.GetConstructors().Where(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == p.t).Single();

            MethodInfo select = typeof(Enumerable).GetMethods()
                .Where(x => x.Name == "Select")
                .Select(x => x.MakeGenericMethod(p.t, rrdt))
                .OrderBy(x => x.GetParameters()[0].ParameterType.GetGenericArguments().Length)
                .First();
            ParameterExpression selectParam = Expression.Parameter(p.t, "x");
            LambdaExpression selectFunc = Expression.Lambda(
                    Expression.New(contr, Expression.Convert(selectParam, p.t)),
                    selectParam
            );

            //TODO: paramaterize & cache
            LambdaExpression l = Expression.Lambda(
                Expression.Call(
                    null,
                    meinf,
                    Expression.Constant(toSave),
                    Expression.Call(
                        null,
                        select,
                        Expression.Convert(Expression.Constant(p.o), typeof(IEnumerable<>).MakeGenericType(p.t)),
                        selectFunc
                    ),
                    Expression.Constant(relationshipName),
                    Expression.Constant(parms),
                    Expression.Constant(forceNodeName1),
                    Expression.Constant(forceNodeName2)
                )
            );
            return ((ICypherFluentQuery query, Dictionary<string, (string, bool)> ids))l.Compile().DynamicInvoke();
        }

        private static (ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) _SaveNodeRelationship<T>(ReflectReadDictionary<T> toSave, (object o, System.Type t) p, string relationshipName, (ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2) where T : INeo4jNode
        {
            if (p.o == null)
            {
                return parms;
            }

            MethodInfo meinf = typeof(DBOps)
                .GetMethod(nameof(DBOps.SaveNodeRelationship), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(typeof(T), p.t);
            ConstructorInfo contr = typeof(ReflectReadDictionary<>).MakeGenericType(p.t).GetConstructors().Where(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == p.t).Single();
            //TODO: paramaterize & cache
            LambdaExpression l = Expression.Lambda(
                Expression.Call(
                    null,
                    meinf,
                    Expression.Constant(toSave),
                    Expression.New(contr, Expression.Convert(Expression.Constant(p.o), p.t)),
                    Expression.Constant(relationshipName),
                    Expression.Constant(parms),
                    Expression.Constant(forceNodeName1),
                    Expression.Constant(forceNodeName2)
                )
            );
            return ((ICypherFluentQuery query, Dictionary<string, (string, bool)> ids))l.Compile().DynamicInvoke();
        }


        private static (Neo4jClient.Cypher.ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) SaveNodeInline<T>(ReflectReadDictionary<T> toSave, (Neo4jClient.Cypher.ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName = null) where T : INeo4jNode
        {
            string id = toSave.Get(x => x.Id);
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString("n");
            }
            toSave.Set(x => x.Id, id);

            string nodeName = forceNodeName ?? id;

            if (parms.ids.TryGetValue(id, out (string, bool) isSaved))
            {
                if (isSaved.Item2)
                {
                    return parms;
                }
                else
                {
                    nodeName = parms.ids[id].Item1;
                    parms.ids[id] = (nodeName, true);
                }
            }
            else
            {
                parms.ids.Add(id, (nodeName, true));
                parms.query = parms.query.Merge($"(_{nodeName}:{typeof(T).QuerySaveLabels()} {{ Id: {Neo4jEncode(toSave.Get(x => x.Id))} }})");
            }

            parms.query = parms.query.Set($"_{nodeName}.__type__ = {Neo4jEncode(typeof(T).FullName)}");

            IEnumerable<Property> props = toSave.propCache.props.Select(x => x.Value)
                .Where(x => !x.neo4JAttributes.Where(y => y is DbIgnoreAttribute).Any())
                .OrderBy(x => typeof(INeo4jNode).IsAssignableFrom(x.info.PropertyType) ? 0 : 1)
                .ToList();
            foreach (Property v in props)
            {
                object o = v.PullValue(toSave.backingInstance);
                System.Type t = v.info.PropertyType;


                System.Type[] enuTs = t.GetInterfaces().Select(x =>
                    {
                        System.Type[] typs = x.GetGenericArguments();
                        return (typs.FirstOrDefault(), typeof(IEnumerable).IsAssignableFrom(x) && typs.Length == 1 && typeof(INeo4jNode).IsAssignableFrom(typs[0]));
                    })
                    .Where(x => x.Item2)
                    .Select(x => x.Item1)
                    .Distinct()
                    .ToArray();
                System.Type enuT = enuTs.FirstOrDefault();
                System.Type enu = enuT != null ? typeof(IEnumerable<>).MakeGenericType(enuT) : null;

                string relationshipName =
                    v.neo4JAttributes.Where(x => x is DbNameAttribute).Cast<DbNameAttribute>().SingleOrDefault()?.Name
                    ?? v.info.Name;
                if (typeof(INeo4jNode).IsAssignableFrom(t))
                {
                    parms = _SaveNodeRelationship(toSave, (o, t), relationshipName, parms, nodeName, $"{nodeName}_{relationshipName}");
                }
                else if (enuT != null)
                {
                    parms = _SaveEnumerableNodeRelationship(toSave, (o, enuT), relationshipName, parms, nodeName, $"{nodeName}_{relationshipName}");
                }
                else
                {
                    parms = SaveValueInline(toSave, v, parms, nodeName);
                }
            }

            return parms;
        }
        private static (Neo4jClient.Cypher.ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) SaveValueInline<T>(ReflectReadDictionary<T> toSave, Property value, (Neo4jClient.Cypher.ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName = null) where T : INeo4jNode
        {
            System.Type t = value.info.PropertyType;
            object o = value.PullValue(toSave.backingInstance);

            string nodeName = forceNodeName ?? toSave.Get(x => x.Id);
            parms.query = parms.query.Set($"_{nodeName}.{value.Name} = {Neo4jEncode(o, t)}");

            return parms;
        }
        private static (Neo4jClient.Cypher.ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) SaveNodeRelationship<T, U>(ReflectReadDictionary<T> toSave, ReflectReadDictionary<U> toSaveB, string relationshipName, (Neo4jClient.Cypher.ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2) where T : INeo4jNode where U : INeo4jNode
        {

            string id1 = toSave.Get(x => x.Id);
            if (string.IsNullOrWhiteSpace(id1))
            {
                id1 = Guid.NewGuid().ToString("n");
            }
            toSave.Set(x => x.Id, id1);

            string id2 = toSaveB.Get(x => x.Id);
            if (string.IsNullOrWhiteSpace(id2))
            {
                id2 = Guid.NewGuid().ToString("n");
            }
            toSaveB.Set(x => x.Id, id2);

            string dupeCheck = $"(_{toSave.Get(x => x.Id)})-[:{relationshipName}]->(_{toSaveB.Get(x => x.Id)})";

            if (parms.ids.TryGetValue(dupeCheck, out (string, bool) isSaved))
            {
                if (isSaved.Item2)
                {
                    return parms;
                }
                else
                {
                    parms.ids[dupeCheck] = (dupeCheck, true);
                }
            }
            else
            {
                parms.ids.Add(dupeCheck, (dupeCheck, true));
            }
            if (!parms.ids.ContainsKey(id1))
            {
                parms.ids.Add(id1, (forceNodeName1, false));
            }
            if (!parms.ids.ContainsKey(id2))
            {
                parms.ids.Add(id2, (forceNodeName2, false));
            }

            parms = SaveNodeInline(toSave, parms, forceNodeName1);
            parms.query = parms.query.Merge($"(_{parms.ids[id1].Item1})-[:{relationshipName}]->(_{parms.ids[id2].Item1}{(parms.ids[id2].Item2? "": $":{typeof(U).QuerySaveLabels()}")})");
            parms = SaveNodeInline(toSaveB, parms, forceNodeName2);

            return parms;
        }
        private static (Neo4jClient.Cypher.ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) SaveEnumerableNodeRelationship<T, U>(ReflectReadDictionary<T> toSave, IEnumerable<ReflectReadDictionary<U>> toSaveB, string relationshipName, (Neo4jClient.Cypher.ICypherFluentQuery query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2) where T : INeo4jNode where U : INeo4jNode
        {
            int i = 0;
            foreach (var v in toSaveB)
            {
                parms = SaveNodeRelationship(toSave, v, relationshipName, parms, forceNodeName1, $"{forceNodeName2}_{i}");
                i++;
            }
            return parms;
        }
        
        private static string Neo4jEncode(object o, System.Type t = null)
        {
            if (t == null && o != null)
            {
                t = o.GetType();
            }
            return JsonConvert.SerializeObject(o);

            //if(t == typeof(bool))
            //{
            //    return ((bool)o) ? "True" : "False";
            //}else if (
            //    t == typeof(sbyte) || t == typeof(short) || t == typeof(int) || t == typeof(long) ||
            //    t == typeof(byte) || t == typeof(ushort) || t == typeof(uint) || t == typeof(ulong) ||
            //    t == typeof(float) || t == typeof(double) || t == typeof(decimal)
            //)
            //{
            //    return o.ToString();
            //}
        }
    }
}
