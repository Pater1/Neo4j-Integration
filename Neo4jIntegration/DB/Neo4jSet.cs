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

                    return h.Relationships
                        .Select((x,i) => (nodes[i].Id, nodes[i+1].Id, x.Type))
                        .Cast<(string parentID, string childID, string relationship)>();
                }).ToList();

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

                var groupDedupe = withProps.GroupBy(x => x.Item1 + x.Item2).Select(x => x.First()).GroupBy(x => (x.Value, x.Item1));

                foreach (var g in groupDedupe)
                {
                    if (!g.Key.Value.isCollection)
                    {
                        var v = g.Single();
                        v.Item3.PushValue(nodeHeap[v.Item1], nodeHeap[v.Item2]);
                    }
                    else
                    {
                        System.Type t = g.Key.Value.info.PropertyType.GetGenericArguments()[0];
                        System.Type lstT = typeof(List<>).MakeGenericType(t);
                        ConstructorInfo cotr = lstT.GetConstructors().Where(x => x.GetParameters().Length == 0).Single();

                        Type enu = typeof(Enumerable);
                        MethodInfo selct = enu.GetMethods()
                            .Where(x => x.Name == "Select")
                            .OrderBy(x => x.GetParameters().Length)
                            .First()
                            .MakeGenericMethod(typeof(string), t);
                        MethodInfo toLst = enu.GetMethods()
                            .Where(x => x.Name == "ToList")
                            .OrderBy(x => x.GetParameters().Length)
                            .First()
                            .MakeGenericMethod(t);

                        Type ndhpTyp = nodeHeap.GetType();

                        ParameterExpression ndhp = Expression.Parameter(ndhpTyp, "ndhp");
                        ParameterExpression ky = Expression.Parameter(typeof(string), "ky");
                        var selExpr = Expression.Lambda(
                            Expression.Convert(Expression.Property(ndhp, "Item", ky),t),
                            ky
                        );

                        var expr = Expression.Lambda(
                            Expression.Call(
                                null,
                                toLst,
                                Expression.Call(
                                    null,
                                    selct,
                                    Expression.Constant(g.Select(x => x.Item2)),
                                    selExpr
                                )
                            ),
                            ndhp
                        );

                        object psh = expr.Compile().DynamicInvoke(nodeHeap);

                        g.Key.Item1.PushValue(nodeHeap[g.Key.Item2], 
                           psh);
                    }
                }

                var ret = nodeHeap
                    .Select(x => x.Value)
                    .Where(x => x is T)
                    .Cast<T>()
                    .Distinct()
                    .Select(x => LiveDbObject<T>.Build(x, clientFactory, LiveObjectMode.LiveWrite | LiveObjectMode.DeferedRead))
                    .ToArray();

                return ret;
            }
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
                    internalQ = internalQ.AppendLine($"OPTIONAL MATCH p{PathCount} = ({parentName})-[:{v.Item2.neo4JAttributes.Select(x => x as DbNameAttribute).Where(x => x != null).FirstOrDefault()?.Name ?? v.Item2.info.Name}]->({v.Item1})");
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
        public TypeWrappedCypherFluentQuery<T, V> IncludeCollection<U, V>(Expression<Func<T, IEnumerable<U>>> p, Expression<Func<U, V>> q)
            //where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            return rQ.BackCopy(this).ThenIncludeCollection(p, q);
        }
        public TypeWrappedCypherFluentQuery<T, V> ThenIncludeCollection<U, V>(Expression<Func<S, IEnumerable<U>>> p, Expression<Func<U, V>> q)
            //where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            TypeWrappedCypherFluentQuery<T, IEnumerable<U>> s1 = (TypeWrappedCypherFluentQuery<T, IEnumerable<U>>)this.ThenInclude(p);
            TypeWrappedCypherFluentQuery<T, U> s2 = s1.SubBuild<U>(s1.quearyName);
            TypeWrappedCypherFluentQuery<T, V> s3 = (TypeWrappedCypherFluentQuery<T, V>)s2.ThenInclude(q);
            return s3;
        }

        public TypeWrappedCypherFluentQuery<T, U> IncludeCollection<U>(Expression<Func<T, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        {
            return this.IncludeCollection<U, U>(p, x => x);
        }
        public TypeWrappedCypherFluentQuery<T, U> ThenIncludeCollection<U>(Expression<Func<S, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        {
            return this.ThenIncludeCollection<U, U>(p, x => x);
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
