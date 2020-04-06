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
//                                    qName, valueWritten, inCurQ
using NodeStatus = System.ValueTuple<string, bool, bool>;
//                               WorkingQ                                                                                                                                         DebugQ 
using Params = System.ValueTuple<System.Text.StringBuilder, System.Collections.Generic.Dictionary<string, System.ValueTuple<string, bool, bool>>, Neo4j.Driver.IAsyncTransaction, System.Text.StringBuilder, System.Collections.Generic.List<Neo4j.Driver.Query>>;

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
            Dictionary<string, NodeStatus> savedIds = new Dictionary<string, NodeStatus>();
            using (IDriver graphClient = graphClientFactory())
            {
                object o = prop.PullValue(parent.BackingInstance);

                StringBuilder sb = new StringBuilder();

                //Query q = new Query(parms.Item1.ToString());
                await graphClient.AsyncSession().WriteTransactionAsync(async x =>
                {
                    try
                    {
                        Params parms = await _SaveNodeRelationship(parent, (o, o.GetType()), prop.jsonName, (sb, savedIds, x, new StringBuilder(), new List<Query>()), "parent", "child", graphClientFactory);
                        //await x.RunAsync(q);
                        await Task.WhenAll(parms.Item5.Select(async y => await x.RunAsync(y)));
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
            Dictionary<string, NodeStatus> savedIds = new Dictionary<string, NodeStatus>();
            using (IDriver graphClient = graphClientFactory())
            {

                StringBuilder sb = new StringBuilder();

                //Query q = new Query(parms.Item1.ToString());
                await graphClient.AsyncSession().WriteTransactionAsync(async x =>
                {
                    try
                    {
                        Params parms = await SaveNodeInline(toSave, (sb, savedIds, x, new StringBuilder(), new List<Query>()), "root", graphClientFactory);
                        //await x.RunAsync(q);
                        await Task.WhenAll(parms.Item5.Select(async y => await x.RunAsync(y)));
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
            Dictionary<string, NodeStatus> savedIds = new Dictionary<string, NodeStatus>();
            using (IDriver graphClient = graphClientFactory())
            {
                object o = prop.PullValue(parent.BackingInstance);

                StringBuilder sb = new StringBuilder();


                //Query q = new Query(parms.Item1.ToString());
                await graphClient.AsyncSession().WriteTransactionAsync(async x =>
                {
                    try
                    {
                        Params parms = await _SaveEnumerableNodeRelationship(parent, (o, o.GetType()), prop.jsonName, (sb, savedIds, x, new StringBuilder(), new List<Query>()), "parent", "child", graphClientFactory);
                        //await x.RunAsync(q);
                        await Task.WhenAll(parms.Item5.Select(async y => await x.RunAsync(y)));
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

        private static readonly Dictionary<System.Type, Delegate> _SENRCache = new Dictionary<System.Type, Delegate>();
        private static async Task<Params> _SaveEnumerableNodeRelationship<T>(LiveDbObject<T> toSave, (object o, System.Type t) p, string relationshipName, Params parms, string forceNodeName1, string forceNodeName2, Func<IDriver> graphClientFactory)
        {
            if (p.o == null)
            {
                return parms;
            }
            Delegate del;
            if (!_SNNCache.TryGetValue(p.t, out del))
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

                ParameterExpression tsv = Expression.Parameter(typeof(LiveDbObject<T>));
                ParameterExpression po = Expression.Parameter(typeof(object));
                ParameterExpression relNam = Expression.Parameter(typeof(string));
                ParameterExpression prmz = Expression.Parameter(typeof(Params));
                ParameterExpression fnn1 = Expression.Parameter(typeof(string));
                ParameterExpression fnn2 = Expression.Parameter(typeof(string));
                ParameterExpression gcf = Expression.Parameter(typeof(Func<IDriver>));

                //TODO: paramaterize & cache
                LambdaExpression l = Expression.Lambda(
                    Expression.Call(
                        null,
                        meinf,
                        tsv,//Expression.Constant(toSave),
                        Expression.Call(
                            null,
                            select,
                            Expression.Convert(
                                po,//Expression.Constant(p.o), 
                                typeof(IEnumerable<>).MakeGenericType(p.t)
                            ),
                            selectFunc
                        ),
                        //Expression.Constant(relationshipName),
                        //Expression.Constant(parms),
                        //Expression.Constant(forceNodeName1),
                        //Expression.Constant(forceNodeName2),
                        //Expression.Constant(graphClientFactory)

                        relNam,
                        prmz,
                        fnn1,
                        fnn2,
                        gcf
                    ),
                    tsv,
                    po,
                    relNam,
                    prmz,
                    fnn1,
                    fnn2,
                    gcf
                );
                del = l.Compile();
                _SENRCache[p.t] = del;
            }
            return await (Task<Params>)del.DynamicInvoke(toSave, p.o, relationshipName, parms, forceNodeName1, forceNodeName2, graphClientFactory);
        }
        private static readonly Dictionary<System.Type, Delegate> _SNNCache = new Dictionary<System.Type, Delegate>();
        private static async Task<Params> _SaveNodeRelationship<T>(LiveDbObject<T> toSave, (object o, System.Type t) p, string relationshipName, Params parms, string forceNodeName1, string forceNodeName2, Func<IDriver> graphClientFactory)
        {
            if (p.o == null)
            {
                return parms;
            }

            Delegate del;
            if (!_SNNCache.TryGetValue(p.t, out del))
            {
                MethodInfo meinf = typeof(DBOps)
                    .GetMethod(nameof(DBOps.SaveNodeRelationship), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(typeof(T), p.t);
                MethodInfo contr = typeof(LiveDbObject<>).MakeGenericType(p.t).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(x => x.Name == "Build").Single();

                ParameterExpression tsv = Expression.Parameter(typeof(LiveDbObject<T>));
                ParameterExpression po = Expression.Parameter(typeof(object));
                ParameterExpression relNam = Expression.Parameter(typeof(string));
                ParameterExpression prmz = Expression.Parameter(typeof(Params));
                ParameterExpression fnn1 = Expression.Parameter(typeof(string));
                ParameterExpression fnn2 = Expression.Parameter(typeof(string));
                ParameterExpression gcf = Expression.Parameter(typeof(Func<IDriver>));
                ParameterExpression lom = Expression.Parameter(typeof(LiveObjectMode));

                //TODO: paramaterize & cache
                LambdaExpression l = Expression.Lambda(
                    Expression.Call(
                        null,
                        meinf,
                        tsv,//Expression.Constant(toSave),
                        Expression.Call(null, contr,
                            Expression.Convert(
                                po,
                                p.t
                            ),
                            gcf,
                            lom//Expression.Constant(LiveObjectMode.IgnoreWrite)
                        ),
                        //Expression.New(contr, Expression.Convert(Expression.Constant(p.o), p.t), Expression.Constant(graphClientFactory), Expression.Constant(LiveObjectMode.IgnoreWrite)),

                        //Expression.Constant(relationshipName),
                        //Expression.Constant(parms),
                        //Expression.Constant(forceNodeName1),
                        //Expression.Constant(forceNodeName2),
                        //Expression.Constant(graphClientFactory)

                        relNam,
                        prmz,
                        fnn1,
                        fnn2,
                        gcf
                    ),
                    tsv,
                    po,
                    relNam,
                    prmz,
                    fnn1,
                    fnn2,
                    gcf,
                    lom
                );
                del = l.Compile();
                _SNNCache[p.t] = del;
            }

            return await (Task<Params>)del.DynamicInvoke(toSave, p.o, relationshipName, parms, forceNodeName1, forceNodeName2, graphClientFactory, LiveObjectMode.Ignore);
        }

        private static async Task<Params> SaveNodeInline<T>(LiveDbObject<T> toSave, Params parms, string forceNodeName, Func<IDriver> graphClientFactory)
        {
            string id = toSave["Id"].ToString();
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString("n");
                toSave["Id"] = id;
            }

            string nodeName = forceNodeName ?? id;

            //if (parms.Item2.TryGetValue(id, out NodeStatus isSaved))
            //{
            //    nodeName = parms.Item2[id].Item1;
            //    parms.Item2[id] = (nodeName, true, true);

            //    if (!isSaved.Item3)
            //    {
            //        parms.Item1 = parms.Item1.AppendLine($"MERGE (_{nodeName}:{typeof(T).QuerySaveLabels()} {{ Id: {Neo4jEncode(toSave["Id"].ToString())} }})");
            //    }

            //    if (isSaved.Item2)
            //    {
            //        return parms;
            //    }
            //}
            //else
            //{
            //    parms.Item2.Add(id, (nodeName, true, true));
            //    parms.Item1 = parms.Item1.AppendLine($"MERGE (_{nodeName}:{typeof(T).QuerySaveLabels()} {{ Id: {Neo4jEncode(toSave["Id"].ToString())} }})");
            //}

            if (parms.Item2.TryGetValue(id, out NodeStatus isSaved))
            {
                return parms;
            }
            else
            {
                parms.Item2[id] = (nodeName, true, true);
            }

            parms.Item1 = parms.Item1.AppendLine($"MERGE (_{nodeName}:{typeof(T).QuerySaveLabels()} {{ Id: {Neo4jEncode(toSave["Id"].ToString())} }})");
            parms.Item1 = parms.Item1.AppendLine($"SET _{nodeName}.__type__ = {Neo4jEncode(typeof(T).FullName)}");

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


            if (parms.Item3 != null)
            {
                Query q = new Query(parms.Item1.ToString());
                //await parms.Item3.RunAsync(q);
                //parms.Item5.Add(parms.Item3.RunAsync(q));
                parms.Item5.Add(q);
                if (parms.Item4 != null)
                {
                    parms.Item4.AppendLine();
                    parms.Item4.AppendLine(q.Text);
                }
                parms.Item1.Clear();
                //parms.Item2 = parms.Item2.ToDictionary(kv => kv.Key, kv => new NodeStatus(kv.Value.Item1, kv.Value.Item2, false));

                //var keys = parms.Item2.Keys.ToList();
                //foreach (var k in keys)
                //{
                //    var v = parms.Item2[k];
                //    parms.Item2[k] = new NodeStatus(v.Item1, v.Item2, false);
                //}
            }

            return parms;
        }
        private static async Task<Params> SaveValueInline<T>(LiveDbObject<T> toSave, Property value, Params parms, string forceNodeName, Func<IDriver> graphClientFactory)
        {
            System.Type t = value.info.PropertyType;
            object o = value.PullValue(toSave.BackingInstance);

            string nodeName = forceNodeName ?? toSave["Id"].ToString();
            parms.Item1 = parms.Item1.AppendLine($"SET _{nodeName}.{value.infoName} = {Neo4jEncode(o, t)}");

            return parms;
        }
        private static async Task<Params> SaveNodeRelationship<T, U>(LiveDbObject<T> toSave, LiveDbObject<U> toSaveB, string relationshipName, Params parms, string forceNodeName1, string forceNodeName2, Func<IDriver> graphClientFactory) where U : INeo4jNode
        {

            string id1 = toSave["Id"]?.ToString();
            if (string.IsNullOrWhiteSpace(id1))
            {
                id1 = Guid.NewGuid().ToString("n");
                toSave["Id"] = id1;
            }

            //bool setId2 = false;
            string id2 = toSaveB["Id"]?.ToString();
            if (string.IsNullOrWhiteSpace(id2))
            {
                id2 = Guid.NewGuid().ToString("n");
                //setId2 = true;
                toSaveB["Id"] = id2;
            }

            string dupeCheck = "_" + (ulong)($"(_{toSave["Id"]})-[:{relationshipName}]->(_{toSaveB["Id"]})").GetHashCode();

            //if (parms.Item2.TryGetValue(dupeCheck, out NodeStatus isSaved))
            //{
            //    if (isSaved.Item2)
            //    {
            //        return parms;
            //    }
            //    else
            //    {
            //        parms.Item2[dupeCheck] = (dupeCheck, true, true);
            //    }
            //}
            //else
            //{
            //    parms.Item2.Add(dupeCheck, (dupeCheck, true, true));
            //}
            //if (!parms.Item2.ContainsKey(id1))
            //{
            //    parms.Item2.Add(id1, (forceNodeName1, false, true));
            //}
            //if (!parms.Item2.ContainsKey(id2))
            //{
            //    parms.Item2.Add(id2, (forceNodeName2, false, true));
            //}

            if (parms.Item2.TryGetValue(dupeCheck, out NodeStatus isSaved))
            {
                return parms;
            }
            else
            {
                parms.Item2[dupeCheck] = (dupeCheck, true, true);
            }

            parms = await SaveNodeInline(toSave, parms, forceNodeName1, graphClientFactory);
            parms = await SaveNodeInline(toSaveB, parms, forceNodeName2, graphClientFactory);

            parms.Item1 = parms.Item1.AppendLine($"MERGE (_{parms.Item2[id2].Item1}:{typeof(U).QuerySaveLabels()} {{ Id: {Neo4jEncode(toSaveB["Id"].ToString())} }})");
            parms.Item1 = parms.Item1.AppendLine($"MERGE (_{parms.Item2[id1].Item1})-[{dupeCheck}:{relationshipName}]->(_{parms.Item2[id2].Item1})");

            //if (setId2)
            //{
            //    parms.Item1 = parms.Item1.AppendLine($"MERGE (_{parms.Item2[id1].Item1})-[{dupeCheck}:{relationshipName}]->(_{parms.Item2[id2].Item1}{(parms.Item2[id2].Item2 ? "" : $":{typeof(U).QuerySaveLabels()}")})");
            //}
            //else
            //{
            //    if (!parms.Item2[id2].Item2)
            //    {
            //        parms.Item1 = parms.Item1.AppendLine($"MERGE (_{parms.Item2[id2].Item1}:{typeof(U).QuerySaveLabels()} {{ Id: {Neo4jEncode(toSaveB["Id"].ToString())} }})");
            //    }
            //    parms.Item1 = parms.Item1.AppendLine($"MERGE (_{parms.Item2[id1].Item1})-[{dupeCheck}:{relationshipName}]->(_{parms.Item2[id2].Item1})");
            //}
            //parms.Item1 = parms.Item1.AppendLine($"SET {dupeCheck}.__type__ = \"{relationshipName}\"");

            if (parms.Item3 != null)
            {
                Query q = new Query(parms.Item1.ToString());
                //await parms.Item3.RunAsync(q);
                parms.Item5.Add(q);
                //parms.Item5.Add(parms.Item3.RunAsync(q));
                if (parms.Item4 != null)
                {
                    parms.Item4.AppendLine();
                    parms.Item4.AppendLine(q.Text);
                }
                parms.Item1.Clear();

                //Dictionary<string, NodeStatus>.KeyCollection keys = parms.Item2.Keys;
                //var keys = parms.Item2.Keys.ToList();
                //foreach (var k in keys)
                //{
                //    var v = parms.Item2[k];
                //    parms.Item2[k] = new NodeStatus(v.Item1, v.Item2, false);
                //}
            }

            return parms;
        }
        private static async Task<Params> SaveEnumerableNodeRelationship<T, U>(LiveDbObject<T> toSave, IEnumerable<LiveDbObject<U>> toSaveB, string relationshipName, Params parms, string forceNodeName1, string forceNodeName2, Func<IDriver> graphClientFactory) where U : INeo4jNode
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

            return JsonConvert.DeserializeObject(value.ToString(), propertyType);
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
