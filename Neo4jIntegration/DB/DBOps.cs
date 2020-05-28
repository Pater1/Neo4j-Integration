using Newtonsoft.Json;
using Neo4jIntegration.Models;
using Neo4jIntegration.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Neo4jIntegration.Reflection;
using System.Linq;
using Neo4jIntegration.Attributes;
using static Neo4jIntegration.Reflection.ReflectionCache;
using System.Collections;
using System.Linq.Expressions;
using Neo4j.Driver;
using System.Threading.Tasks;

namespace Neo4jIntegration.DB
{
    public static class DBOps
    {
        private static async Task DbCleanup(this IAsyncTransaction transaction)
        {
            //            await transaction.RunAsync(@"
            //MATCH (n)
            //WHERE NOT (n)<--()
            //AND NOT ""Independant"" IN LABELS(n)
            //OPTIONAL MATCH(n2:ParentRequired)<--(n)
            //OPTIONAL MATCH(n3:ParentRequired)<--(n2)
            //DETACH DELETE n, n2, n3
            //            ");
        }
        public static async Task SaveSubnode<T, TValue>(LiveDbObject<T> parent, Property prop, TValue value, Func<IDriver> graphClientFactory)
        {
            Dictionary<string, (string, bool)> savedIds = new Dictionary<string, (string, bool)>();
            using (IDriver graphClient = graphClientFactory())
            {
                object o = prop.PullValue(parent.BackingInstance);

                StringBuilder sb = new StringBuilder();
                
                (StringBuilder query, Dictionary<string, (string, bool)> ids) parms = await _SaveNodeRelationship(parent, (o, o.GetType()), prop.jsonName, (sb, savedIds), "parent", "child", graphClientFactory);

                Query q = new Query(parms.query.ToString());
                await graphClient.AsyncSession().WriteTransactionAsync(async x =>
                {
                    try
                    {
                        await x.RunAsync(q);
                        await x.DbCleanup();
                        await x.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await x.RollbackAsync();
                        throw ex;
                    }
                });
            }
        }
        public static async Task SaveValue<T, TValue>(LiveDbObject<T> liveDbObject, Property prop, TValue value, Func<IDriver> graphClientFactory)
        {
            using (IDriver graphClient = graphClientFactory())
            {
                StringBuilder cypherFluentQuery = new StringBuilder();

                cypherFluentQuery = cypherFluentQuery.AppendLine($"MERGE (objec:{typeof(T).QuerySaveLabels()} {{ Id: {Neo4jEncode(liveDbObject["Id"])} }})");
                cypherFluentQuery = cypherFluentQuery.AppendLine($"SET objec.{prop.jsonName} = {Neo4jEncode(value)}");

                using (IDriver driver = graphClientFactory())
                {
                    Query q = new Query(cypherFluentQuery.ToString());
                    await driver.AsyncSession().WriteTransactionAsync(async x =>
                    {
                        try
                        {
                            await x.RunAsync(q);
                            await x.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            await x.RollbackAsync();
                            throw ex;
                        }
                    });
                }
            }
        }
        public static async Task SaveNode<T>(LiveDbObject<T> toSave, Func<IDriver> graphClientFactory)
        {
            Dictionary<string, (string, bool)> savedIds = new Dictionary<string, (string, bool)>();
            using (IDriver graphClient = graphClientFactory())
            {

                StringBuilder sb = new StringBuilder();
                (StringBuilder query, Dictionary<string, (string, bool)> ids) parms
                    = await SaveNodeInline(toSave, (sb, savedIds), "root", graphClientFactory);

                Query q = new Query(parms.query.ToString());
                await graphClient.AsyncSession().WriteTransactionAsync(async x =>
                {
                    try
                    {
                        await x.RunAsync(q);
                        await x.DbCleanup();
                        await x.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await x.RollbackAsync();
                        throw ex;
                    }
                });
            }
        }
        public static async Task SaveCollection<T, TValue>(LiveDbObject<T> parent, Property prop, TValue value, Func<IDriver> graphClientFactory)
        {
            Dictionary<string, (string, bool)> savedIds = new Dictionary<string, (string, bool)>();
            using (IDriver graphClient = graphClientFactory())
            {
                object o = prop.PullValue(parent.BackingInstance);
                
                StringBuilder sb = new StringBuilder();

                (StringBuilder query, Dictionary<string, (string, bool)> ids) parms = 
                    await _SaveEnumerableNodeRelationship(parent, (o, o.GetType()), prop.jsonName, (sb, savedIds), "parent", "child", graphClientFactory);

                Query q = new Query(parms.query.ToString());
                await graphClient.AsyncSession().WriteTransactionAsync(async x =>
                {
                    try
                    {
                        await x.RunAsync(q);
                        await x.DbCleanup();
                        await x.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await x.RollbackAsync();
                        throw ex;
                    }
                });
            }
        }

        private static readonly Dictionary<(System.Type, System.Type), Delegate> _SaveNodeRelationship_DelegateCache = new Dictionary<(System.Type, System.Type), Delegate>();
        private static async Task<(StringBuilder query, Dictionary<string, (string, bool)> ids)> _SaveEnumerableNodeRelationship<T>(LiveDbObject<T> toSave, (object o, System.Type t) p, string relationshipName, (StringBuilder query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2, Func<IDriver> graphClientFactory)
        {
            if (p.o == null)
            {
                return parms;
            }

            Delegate comp;
            lock (_SaveNodeRelationship_DelegateCache)
            {
                if (!_SaveNodeRelationship_DelegateCache.TryGetValue((typeof(LiveDbObject<T>), typeof(IEnumerable<>).MakeGenericType(p.t)), out comp))
                {
                    MethodInfo meinf = typeof(DBOps)
                .GetMethod(nameof(DBOps.SaveEnumerableNodeRelationship), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(typeof(T), p.t);
                    System.Type rrdt = typeof(LiveDbObject<>).MakeGenericType(p.t);

                    MethodInfo contr = typeof(LiveDbObject<>).MakeGenericType(p.t).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(x => x.Name == "Build").Single(); ;
                    //ConstructorInfo contr = rrdt.GetConstructors().Where(x => x.GetParameters().Length == 3 && x.GetParameters()[0].ParameterType == p.t).Single();

                    MethodInfo select = typeof(Enumerable).GetMethods()
                        .Where(x => x.Name == "Select")
                        .Select(x => x.MakeGenericMethod(p.t, rrdt))
                        .OrderBy(x => x.GetParameters()[0].ParameterType.GetGenericArguments().Length)
                        .First();
                    ParameterExpression selectParam = Expression.Parameter(p.t, "x");
                    LambdaExpression selectFunc = Expression.Lambda(
                            Expression.Call(null, contr, Expression.Convert(selectParam, p.t), Expression.Constant(graphClientFactory), Expression.Constant(LiveObjectMode.IgnoreWrite)),
                            selectParam
                    );

                    ParameterExpression toSaveExp = Expression.Parameter(typeof(LiveDbObject<T>));
                    ParameterExpression oExp = Expression.Parameter(typeof(object));
                    ParameterExpression graphClientFactoryExp = Expression.Parameter(typeof(Func<IDriver>));
                    ParameterExpression relationshipNameExp = Expression.Parameter(typeof(string));
                    ParameterExpression parmsExp = Expression.Parameter(parms.GetType());
                    ParameterExpression forceNodeName1Exp = Expression.Parameter(typeof(string));
                    ParameterExpression forceNodeName2Exp = Expression.Parameter(typeof(string));

                    //TODO: paramaterize & cache
                    LambdaExpression l = Expression.Lambda(
                        Expression.Call(
                            null,
                            meinf,
                            toSaveExp,//Expression.Constant(toSave),
                            Expression.Call(
                                null,
                                select,
                                Expression.Convert(
                                        oExp,//Expression.Constant(p.o),
                                        typeof(IEnumerable<>).MakeGenericType(p.t)),
                                selectFunc
                            ),
                            relationshipNameExp,//Expression.Constant(relationshipName),
                            parmsExp,//Expression.Constant(parms),
                            forceNodeName1Exp,//Expression.Constant(forceNodeName1),
                            forceNodeName2Exp,//Expression.Constant(forceNodeName2),
                            graphClientFactoryExp//Expression.Constant(graphClientFactory)
                        ),
                        toSaveExp,
                        oExp,
                        graphClientFactoryExp,
                        relationshipNameExp,
                        parmsExp,
                        forceNodeName1Exp,
                        forceNodeName2Exp
                    );

                    comp = l.Compile();
                }
            }
            return await (Task<(StringBuilder query, Dictionary<string, (string, bool)> ids)>)comp
                .DynamicInvoke(toSave, p.o, graphClientFactory, relationshipName, parms, forceNodeName1, forceNodeName2);
        }
        private static async Task<(StringBuilder query, Dictionary<string, (string, bool)> ids)> _SaveNodeRelationship<T>(LiveDbObject<T> toSave, (object o, System.Type t) p, string relationshipName, (StringBuilder query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2, Func<IDriver> graphClientFactory)
        {
            if (p.o == null)
            {
                return parms;
            }

            Delegate comp;
            lock (_SaveNodeRelationship_DelegateCache)
            {
                if (!_SaveNodeRelationship_DelegateCache.TryGetValue((typeof(LiveDbObject<T>), p.t), out comp))
                {
                    MethodInfo meinf = typeof(DBOps)
                        .GetMethod(nameof(DBOps.SaveNodeRelationship), BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(typeof(T), p.t);
                    MethodInfo contr = typeof(LiveDbObject<>).MakeGenericType(p.t).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(x => x.Name == "Build").Single();

                    ParameterExpression toSaveExp = Expression.Parameter(typeof(LiveDbObject<T>));
                    ParameterExpression oExp = Expression.Parameter(typeof(object));
                    ParameterExpression graphClientFactoryExp = Expression.Parameter(typeof(Func<IDriver>));
                    ParameterExpression relationshipNameExp = Expression.Parameter(typeof(string));
                    ParameterExpression parmsExp = Expression.Parameter(parms.GetType());
                    ParameterExpression forceNodeName1Exp = Expression.Parameter(typeof(string));
                    ParameterExpression forceNodeName2Exp = Expression.Parameter(typeof(string));

                    LambdaExpression l = Expression.Lambda(
                        Expression.Call(
                            null,
                            meinf,
                            toSaveExp,//Expression.Constant(toSave),
                            Expression.Call(
                                null,
                                contr,
                                Expression.Convert(
                                        oExp,//Expression.Constant(p.o),
                                        p.t),
                                graphClientFactoryExp,//Expression.Constant(graphClientFactory), 
                                Expression.Constant(LiveObjectMode.IgnoreWrite)
                            ),
                            relationshipNameExp,//Expression.Constant(relationshipName),
                            parmsExp,//Expression.Constant(parms),
                            forceNodeName1Exp,//Expression.Constant(forceNodeName1),
                            forceNodeName2Exp,//Expression.Constant(forceNodeName2),
                            graphClientFactoryExp//Expression.Constant(graphClientFactory)
                        ),
                        toSaveExp,
                        oExp,
                        graphClientFactoryExp,
                        relationshipNameExp,
                        parmsExp,
                        forceNodeName1Exp,
                        forceNodeName2Exp
                    );
                    comp = l.Compile();

                    _SaveNodeRelationship_DelegateCache.Add((typeof(LiveDbObject<T>), p.t), comp);
                }
            }
            return await (Task<(StringBuilder query, Dictionary<string, (string, bool)> ids)>)
                comp.DynamicInvoke(toSave, p.o, graphClientFactory, relationshipName, parms, forceNodeName1, forceNodeName2);
        }

        private static async Task<(StringBuilder query, Dictionary<string, (string, bool)> ids)> SaveNodeInline<T>(LiveDbObject<T> toSave, (StringBuilder query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName, Func<IDriver> graphClientFactory)
        {
            string id = toSave["Id"].ToString();
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString("n");
            }
            toSave["Id"] = id;

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
                parms.query = parms.query.AppendLine($"MERGE (_{nodeName}:{typeof(T).QuerySaveLabels()} {{ Id: {Neo4jEncode(toSave["Id"].ToString())} }})");
            }

            parms.query = parms.query.AppendLine($"SET _{nodeName}.__type__ = {Neo4jEncode(typeof(T).FullName)}");

            IEnumerable<Property> props = LiveDbObject<T>.PropCache.props.Select(x => x.Value)
                .Where(x => !x.neo4JAttributes.Where(y => y is DbIgnoreAttribute).Any())
                .OrderBy(x => typeof(INeo4jNode).IsAssignableFrom(x.info.PropertyType) ? 0 : 1)
                .ToList();
            foreach (Property v in props)
            {
                object o = v.PullValue(toSave.BackingInstance);
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

                string relationshipName = v.jsonName;
                if (typeof(INeo4jNode).IsAssignableFrom(t))
                {
                    parms = await _SaveNodeRelationship(toSave, (o, t), relationshipName, parms, nodeName, $"{nodeName}_{relationshipName}", graphClientFactory);
                }
                else if (enuT != null)
                {
                    parms = await _SaveEnumerableNodeRelationship(toSave, (o, enuT), relationshipName, parms, nodeName, $"{nodeName}_{relationshipName}", graphClientFactory);
                }
                else
                {
                    parms = await SaveValueInline(toSave, v, parms, nodeName, graphClientFactory);
                }
            }

            return parms;
        }
        private static async Task<(StringBuilder query, Dictionary<string, (string, bool)> ids)> SaveValueInline<T>(LiveDbObject<T> toSave, Property value, (StringBuilder query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName, Func<IDriver> graphClientFactory)
        {
            System.Type t = value.info.PropertyType;
            object o = value.PullValue(toSave.BackingInstance);

            string nodeName = forceNodeName ?? toSave["Id"].ToString();
            parms.query = parms.query.AppendLine($"SET _{nodeName}.{value.infoName} = {Neo4jEncode(o, t)}");

            return parms;
        }
        private static async Task<(StringBuilder query, Dictionary<string, (string, bool)> ids)> SaveNodeRelationship<T, U>(LiveDbObject<T> toSave, LiveDbObject<U> toSaveB, string relationshipName, (StringBuilder query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2, Func<IDriver> graphClientFactory) where U : INeo4jNode
        {

            string id1 = toSave["Id"]?.ToString();
            if (string.IsNullOrWhiteSpace(id1))
            {
                id1 = Guid.NewGuid().ToString("n");
            }
            toSave["Id"] = id1;

            bool setId2 = false;
            string id2 = toSaveB["Id"]?.ToString();
            if (string.IsNullOrWhiteSpace(id2))
            {
                id2 = Guid.NewGuid().ToString("n");
                setId2 = true;
            }
            toSaveB["Id"] = id2;

            string dupeCheck = "_" + (ulong)($"(_{toSave["Id"].ToString()})-[:{relationshipName}]->(_{toSaveB["Id"].ToString()})").GetHashCode();

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

            parms = await SaveNodeInline(toSave, parms, forceNodeName1, graphClientFactory);
            if (setId2)
            {
                parms.query = parms.query.AppendLine($"MERGE (_{parms.ids[id1].Item1})-[{dupeCheck}:{relationshipName}]->(_{parms.ids[id2].Item1}{(parms.ids[id2].Item2 ? "" : $":{typeof(U).QuerySaveLabels()}")})");
            }
            else
            {
                if (!parms.ids[id2].Item2)
                {
                    parms.query = parms.query.AppendLine($"MERGE (_{parms.ids[id2].Item1}:{typeof(U).QuerySaveLabels()} {{ Id: {Neo4jEncode(toSaveB["Id"].ToString())} }})");
                }
                parms.query = parms.query.AppendLine($"MERGE (_{parms.ids[id1].Item1})-[{dupeCheck}:{relationshipName}]->(_{parms.ids[id2].Item1})");
            }
            parms.query = parms.query.AppendLine($"SET {dupeCheck}.__type__ = \"{relationshipName}\"");
            parms = await SaveNodeInline(toSaveB, parms, forceNodeName2, graphClientFactory);

            return parms;
        }
        private static async Task<(StringBuilder query, Dictionary<string, (string, bool)> ids)> SaveEnumerableNodeRelationship<T, U>(LiveDbObject<T> toSave, IEnumerable<LiveDbObject<U>> toSaveB, string relationshipName, (StringBuilder query, Dictionary<string, (string, bool)> ids) parms, string forceNodeName1, string forceNodeName2, Func<IDriver> graphClientFactory) where U : INeo4jNode
        {
            int i = 0;
            foreach (var v in toSaveB)
            {
                parms = await SaveNodeRelationship(toSave, v, relationshipName, parms, forceNodeName1, $"{forceNodeName2}_{i}", graphClientFactory);
                i++;
            }
            return parms;
        }

        public static object Neo4jDecode(object value, System.Type propertyType)
        {
            bool test = typeof(long).IsAssignableFrom(typeof(byte));
            if (propertyType == typeof(string))
            {
                return value.ToString();
            }
            else if (propertyType == typeof(bool))
            {
                string str = value.ToString().ToLowerInvariant();
                return str == "true" || str == "t";
            }
            else if (propertyType == typeof(DateTime))
            {
                DateTime ret;
                if (!DateTime.TryParse(value.ToString(), out ret))
                {

                }
                return ret;
            }
            else if (propertyType.IsEnum)
            {

                string str = value.ToString();
                if (long.TryParse(str, out long l))
                {
                    System.Type numType = Enum.GetUnderlyingType(propertyType);

                    return Enum.ToObject(propertyType, l);

                    //object num = Convert.ChangeType(value, numType);
                    //return Convert.ChangeType(num, propertyType);
                }
                else if (value is string)
                {
                    return Enum.Parse(propertyType, str, true);
                }
            }

            string strVal = JsonConvert.SerializeObject(value);
            return JsonConvert.DeserializeObject(strVal, propertyType);
        }
        private static string Neo4jEncode(object o, System.Type t = null)
        {
            if (t == null && o != null)
            {
                t = o.GetType();
            }
            return JsonConvert.SerializeObject(o);
        }
    }
}
