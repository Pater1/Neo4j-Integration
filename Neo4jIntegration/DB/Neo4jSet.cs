using Neo4j.Driver;
using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using Neo4jIntegration.Extentions;
using Neo4jIntegration.Models;
using Neo4jIntegration.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4jIntegration
{
    public sealed class Neo4jSet<T> //where T : class, INeo4jNode, ITemplatable<T>, new()
    {
        public static readonly string setName = typeof(T).QuerySaveName();

        public static LiveDbObject<T> SingleValue(string id, Func<IDriver> client)
            => Single(id, client).Results.FirstOrDefault();
        public static IEnumerable<LiveDbObject<T>> AllValue(Func<IDriver> client)
            => All(client).Results;

        public static TypeWrappedCypherFluentQuery<T, T> Single(string id, Func<IDriver> client)
            => All(client).Where(x => (x as INeo4jNode).Id == id);
        public static TypeWrappedCypherFluentQuery<T, T> All(Func<IDriver> client)
        {
            return TypeWrappedCypherFluentQuery<T, T>.Build<T>(client);
        }

    }
    public class TypeWrappedCypherFluentQuery<T, S>
    {
        public static readonly Type t = typeof(T);
        public static readonly string name = typeof(T).QuerySaveName();
        public static readonly string labels = typeof(T).QuerySaveLabels();

        public StringBuilder internalQ { get; set; }
        private TypeWrappedCypherFluentQuery<T, T> rQ { get; set; }
        public List<PropertyInfo> pulledChildNodes { get; set; } = new List<PropertyInfo>();
        public Func<IDriver> clientFactory { get; }
        //public List<ReadQueryParams<T>> readQueryParams { get; internal set; } = new List<ReadQueryParams<T>>();
        public List<string> pulledPaths { get; set; } = new List<string>();

        public string quearyName { get; } = "Rootobj";
        //public Expression<Func<IEnumerable<T>, IEnumerable<S>>> quearyExpression { get; internal set; }

        public IEnumerable<LiveDbObject<T>> Results => this.ReturnAsync().Result;

        public int PathCount { get; set; } = 0;

        public static TypeWrappedCypherFluentQuery<U, U> Build<U>(Func<IDriver> client) //where U : class, INeo4jNode, new()
        {
            TypeWrappedCypherFluentQuery<U, U> ths = new TypeWrappedCypherFluentQuery<U, U>(client);
            ths.rQ = ths;
            //ths.quearyExpression = x => x;
            return ths;
        }
        private TypeWrappedCypherFluentQuery<T, U> SubBuild<U>(string path) //where U : class, INeo4jNode, new()
        {
            return new TypeWrappedCypherFluentQuery<T, U>(internalQ, clientFactory, path)
            {
                PathCount = PathCount,
                pulledChildNodes = pulledChildNodes,
                pulledPaths = pulledPaths,
                //readQueryParams = readQueryParams,
                //quearyExpression = expression,
                rQ = rQ
            };
        }
        private TypeWrappedCypherFluentQuery(StringBuilder internalQuery, Func<IDriver> client, string path)
        {
            internalQ = internalQuery;
            this.clientFactory = client;
            this.quearyName = path;
        }
        private TypeWrappedCypherFluentQuery(Func<IDriver> client)
        {
            //internalQ = client().Cypher.Match($"p0 = ({quearyName}:{labels})");

            internalQ = new StringBuilder();
            internalQ.AppendLine($"MATCH p0 = ({quearyName}:{labels})");
            PathCount++;
            this.clientFactory = client;
        }

        public async Task<IEnumerable<LiveDbObject<T>>> ReturnAsync()
        {
            //StringBuilder sb = new StringBuilder();

            //for (int i = 0; i < PathCount; i++)
            //{
            //    if (i > 0)
            //    {
            //        sb.Append(" + ");
            //    }
            //    sb.Append($"collect(p{i})");
            //}
            //sb.Append($" AS paths{Environment.NewLine}");

            //sb.Append($"UNWIND paths AS ret{Environment.NewLine}");
            //sb.Append($"WITH DISTINCT ret AS dist{Environment.NewLine}");
            //sb.Append($"WITH nodes(dist) + collect(labels(nodes(dist)[0])) + collect(labels(nodes(dist)[1])) + type(relationships(dist)[0]) AS meta{Environment.NewLine}");
            //sb.Append($"WITH collect(meta) AS metas");

            //internalQ = internalQ.AppendLine("WITH " + sb.ToString());
            //internalQ = internalQ.AppendLine("RETURN metas as json");

            internalQ.Append("WITH ");
            for (int i = 0; i < PathCount; i++)
            {
                if (i > 0)
                {
                    internalQ.Append(" + ");
                }
                internalQ.Append($"collect(p{i})");
            }
            internalQ.Append($" AS paths{Environment.NewLine}");

            internalQ.Append($"UNWIND paths AS ret{Environment.NewLine}");
            internalQ = internalQ.AppendLine("RETURN ret as json");

            //var reter = internalQ.Return<string[][]>("metas as json");
            using (IDriver driver = clientFactory())
            {
                Query q = new Query(internalQ.ToString());
                IResultCursor resultCursor = await driver.AsyncSession().RunAsync(q);
                List<IRecord> results = await resultCursor.ToListAsync();

                Dictionary<string, INeo4jNode> nodeHeap = new Dictionary<string, INeo4jNode>();
                var raw = results.SelectMany(x =>
                {
                    var h = x["json"].As<IPath>();

                    List<INeo4jNode> nodes = h.Nodes.Select(a =>
                    {
                        INeo4jNode parentInstance;
                        if (!nodeHeap.TryGetValue(a["Id"].ToString(), out parentInstance))
                        {
                            Type parentType = ReflectionCache.BuildType(a["__type__"].As<string>());
                            parentInstance = Activator.CreateInstance(parentType) as INeo4jNode;

                            ReflectionCache.Type typeData = ReflectionCache.GetTypeData(parentType);
                            foreach (KeyValuePair<string, object> k in a.Properties)
                            {
                                if (typeData.props.TryGetValue(k.Key, out ReflectionCache.Property prop))
                                {
                                    prop.PushValue(parentInstance, DBOps.Neo4jDecode(k.Value, prop.info.PropertyType));
                                }
                            }

                            nodeHeap.Add(a["Id"].ToString(), parentInstance);
                        }
                        return parentInstance;
                    }).ToList();

                    return h.Relationships.Select((x,i) => (x,i))
                        .Join(nodes.Select((x,i) => (x,i)), a => a.i, b => b.i, (a,b) => (a.x,b.x,a.i))
                        .Join(nodes.Select((x,i) => (x,i)), a => a.i, b => b.i-1, (a,b) => (a.Item2,b.x,a.Item1))
                        .Select(a => (a.Item1.Id, a.x.Id, a.Item3.Type))
                        .Cast<(string parentID, string childID, string relationship)>();
                        

                    //List<(INeo4jNode, INeo4jNode, string)> retLst = new List<(INeo4jNode, INeo4jNode, string)>();
                    
                    //return retLst;

                    //var h2 = h.Select(y =>
                    //        y.As<List<object>>()
                    //    )
                    //    .Select(z => new
                    //    {
                    //        parent = z[0].As<INode>(),
                    //        child = z[1].As<INode>(),
                    //        relationship = z[4].As<string>()
                    //    })
                    //    .Select(x =>
                    //    {
                    //        INeo4jNode parentInstance;
                    //        if (!nodeHeap.TryGetValue(x.parent["Id"].ToString(), out parentInstance))
                    //        {
                    //            Type parentType = ReflectionCache.BuildType(x.parent["__type__"].As<string>());
                    //            parentInstance = Activator.CreateInstance(parentType) as INeo4jNode;

                    //            ReflectionCache.Type typeData = ReflectionCache.GetTypeData(parentType);
                    //            foreach (KeyValuePair<string, object> k in x.parent.Properties)
                    //            {
                    //                if (typeData.props.TryGetValue(k.Key, out ReflectionCache.Property prop))
                    //                {
                    //                    prop.PushValue(parentInstance, DBOps.Neo4jDecode(k.Value, prop.info.PropertyType));
                    //                }
                    //            }

                    //            nodeHeap.Add(x.parent["Id"].ToString(), parentInstance);
                    //        }


                    //        INeo4jNode childInstance;
                    //        if (!nodeHeap.TryGetValue(x.child["Id"].ToString(), out childInstance))
                    //        {
                    //            Type childType = ReflectionCache.BuildType(x.child["__type__"].As<string>());
                    //            childInstance = Activator.CreateInstance(childType) as INeo4jNode;

                    //            ReflectionCache.Type typeData = ReflectionCache.GetTypeData(childType);
                    //            foreach (KeyValuePair<string, object> k in x.child.Properties)
                    //            {
                    //                if (typeData.props.TryGetValue(k.Key, out ReflectionCache.Property prop))
                    //                {
                    //                    prop.PushValue(childInstance, DBOps.Neo4jDecode(k.Value, prop.info.PropertyType));
                    //                }
                    //            }

                    //            nodeHeap.Add(x.child["Id"].ToString(), childInstance);
                    //        }

                    //        return (parentInstance, childInstance, x.relationship);
                    //    })
                    //    .ToArray();
                    //return h;
                });

                var withProps = raw.Select(x => (x.Item1, x.Item2,
                    ReflectionCache.GetTypeData(nodeHeap[x.Item1]).props
                        .Where(y =>
                            y.Key == x.Item3
                            ||
                            (
                                y.Value.neo4JAttributes.Where(x => x is DbNameAttribute).Any()
                                &&
                                y.Value.neo4JAttributes.Select(x => x as DbNameAttribute).Where(x => x != null).FirstOrDefault()?.Name
                                ==
                                x.Item3
                            )
                         )
                        .SingleOrDefault()
                        .Value
                )).ToList();

                foreach (var v in withProps)
                {
                    v.Item3.PushValue(nodeHeap[v.Item1], nodeHeap[v.Item2]);
                }

                var ret = nodeHeap
                    .Select(x => x.Value)
                    //.Where(x => !raw.Select(a => a.childID).Contains(x.Key))
                    .Where(x => x is T)
                    .Cast<T>()
                    .Distinct()
                    .Select(x => LiveDbObject<T>.Build(x, clientFactory, LiveObjectMode.LiveWrite | LiveObjectMode.DeferedRead))
                    .ToArray();

                return ret;
            }
        }
        private static object ManualDeserializeNode(IDictionary<string, JToken> retValsJson, Type type, IDictionary<string, object> nodeHeap)
        {
            string key = null;
            if (retValsJson.TryGetValue("id", out JToken kJson))
            {
                key = string.Intern(kJson.ToString());
            }

            if (key != null)
            {
                lock (key)
                {
                    if (!nodeHeap.ContainsKey(key))
                    {
                        object retInstance = ManualDeserializeNode(retValsJson, type);
                        nodeHeap.Add(key, retInstance);
                        return retInstance;
                    }
                    else
                    {
                        return nodeHeap[key];
                    }
                }
            }
            else
            {
                return ManualDeserializeNode(retValsJson, type);
            }
        }
        private static object ManualDeserializeNode(IDictionary<string, JToken> retValsJson, Type retType)
        {
            //Type retType = ReflectionCache.BuildType(typeNames, typeof(INeo4jNode));
            object retInstance = Activator.CreateInstance(retType);
            Dictionary<string, ReflectionCache.Property> retProps = ReflectionCache.GetTypeData(retType).props;
            foreach (
                var kv in
                retValsJson.Join(retProps, x => x.Key, x => x.Key, (a, b) => (a.Value, b.Value))
            )
            {
                object push;
                if (kv.Item1 is JValue)
                {
                    //push = (kv.Item1 as JValue).Value;
                    push = kv.Item1.ToObject(kv.Item2.info.PropertyType);
                }
                else
                {
                    push = kv.Item1.ToObject(kv.Item2.info.PropertyType);
                }
                kv.Item2.PushValue(retInstance, push);
            }
            return retInstance;
        }

        public TypeWrappedCypherFluentQuery<T, T> Where(Expression<Func<T, bool>> delegat)
        {
            return rQ.BackCopy(this).ThenWhere(delegat);
        }
        public TypeWrappedCypherFluentQuery<T, S> ThenWhere(Expression<Func<S, bool>> delegat)
        {
            delegat = Scrub(delegat);
            //internalQ = internalQ.Where(Scrub(delegat));
            return this;
        }
        public TypeWrappedCypherFluentQuery<T, U> Include<U>(Expression<Func<T, U>> p) //where U : class, INeo4jNode, new()
        {
            return rQ.BackCopy(this).ThenInclude(p);
        }

        public TypeWrappedCypherFluentQuery<T, U> ThenInclude<U>(Expression<Func<S, U>> p) //where U : class, INeo4jNode, new()
        {
            p = Scrub(p);

            List<(string, ReflectionCache.Property, MemberExpression)> fullPath = new List<(string, ReflectionCache.Property, MemberExpression)>();

            Expression bdy = p.Body;
            TypeWrappedCypherFluentQuery<T, S> ths = this;
            while (bdy is MemberExpression)
            {
                MemberExpression mExp = bdy as MemberExpression;
                PropertyInfo propInfo = mExp.Member as PropertyInfo;
                ReflectionCache.Property prop = new ReflectionCache.Property(propInfo);

                fullPath.Add((mExp.ToString().Replace('.', '_'), prop, mExp));

                bdy = mExp.Expression;
            }

            fullPath.Reverse();

            foreach (var v in fullPath)
            {
                string[] strPath = v.Item1.Split("_");
                string parentName = strPath.Take(strPath.Length - 1).Aggregate((a, b) => a + "_" + b);

                if (!pulledPaths.Contains(v.Item1))
                {
                    internalQ = internalQ.AppendLine($"OPTIONAL MATCH p{PathCount} = ({parentName})-[:{v.Item2.neo4JAttributes.Select(x => x as DbNameAttribute).Where(x => x != null).FirstOrDefault()?.Name ?? v.Item2.info.Name}]->({v.Item1}:{v.Item2.info.PropertyType.QuerySaveLabels()})");
                    pulledPaths.Add(v.Item1);
                    PathCount++;
                }
            }

            string ultimateChild = fullPath.Any() ? fullPath.Last().Item1 : "";

            return SubBuild<U>(ultimateChild);
        }

        private TypeWrappedCypherFluentQuery<T, S> BackCopy<U>(TypeWrappedCypherFluentQuery<T, U> typeWrappedCypherFluentQuery)
        {
            TypeWrappedCypherFluentQuery<T, S> bccp = this;
            bccp.internalQ = typeWrappedCypherFluentQuery.internalQ;
            bccp.pulledChildNodes = bccp.pulledChildNodes.Union(
                typeWrappedCypherFluentQuery.pulledChildNodes
            ).Distinct().ToList();
            bccp.pulledPaths = bccp.pulledPaths.Union(
                typeWrappedCypherFluentQuery.pulledPaths
            ).Distinct().ToList();
            bccp.PathCount = typeWrappedCypherFluentQuery.PathCount;
            if (bccp is TypeWrappedCypherFluentQuery<T, T>)
            {
                var v = (bccp as TypeWrappedCypherFluentQuery<T, T>);
                //v.quearyExpression = x => x;
                bccp = v as TypeWrappedCypherFluentQuery<T, S>;
            }
            return bccp;
        }
        public TypeWrappedCypherFluentQuery<T, V> IncludeCollection<A, U, V>(Expression<Func<T, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            return rQ.BackCopy(this).ThenIncludeCollection(p, q);
        }
        public TypeWrappedCypherFluentQuery<T, V> ThenIncludeCollection<A, U, V>(Expression<Func<S, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            TypeWrappedCypherFluentQuery<T, A> s1 = (TypeWrappedCypherFluentQuery<T, A>)this.ThenInclude(p);
            TypeWrappedCypherFluentQuery<T, U> s2 = s1.SubBuild<U>(s1.quearyName);
            TypeWrappedCypherFluentQuery<T, V> s3 = (TypeWrappedCypherFluentQuery<T, V>)s2.ThenInclude(q);
            return s3;
        }

        public TypeWrappedCypherFluentQuery<T, U> IncludeCollection<U>(Expression<Func<T, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        {
            return this.IncludeCollection<IEnumerable<U>, U, U>(p, x => x);
        }
        public TypeWrappedCypherFluentQuery<T, U> ThenIncludeCollection<U>(Expression<Func<S, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        {
            return this.ThenIncludeCollection<IEnumerable<U>, U, U>(p, x => x);
        }

        private Expression<F> Scrub<F>(Expression<F> delegat)
        {
            ParameterExpression toRepl = delegat.Parameters.First();

            ParameterExpression wrapParam = Expression.Parameter(toRepl.Type, quearyName);

            var extracted = delegat.Replace<F>(toRepl, wrapParam);

            return Expression<F>.Lambda<F>(
                    extracted.body,
                    extracted.param.Select(x => x == null ? wrapParam : x)
                );
        }
    }
}
