using Neo4j.Driver.V1;
using Neo4jClient;
using Neo4jClient.Cypher;
using Neo4jClient.Transactions;
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

        public static ReflectReadDictionary<T> SingleValue(string id, ITransactionalGraphClient client)
            => Single(id, client).Results.FirstOrDefault();
        public static IEnumerable<ReflectReadDictionary<T>> AllValue(ITransactionalGraphClient client)
            => All(client).Results;

        public static ITypeWrappedCypherFluentQuery<T, T> Single(string id, ITransactionalGraphClient client)
            => All(client).Where(x => (x as INeo4jNode).Id == id);
        public static ITypeWrappedCypherFluentQuery<T, T> All(ITransactionalGraphClient client)
        {
            return ColdTypeWrappedCypherFluentQuery<T, T>.Build<T>(client);
        }

    }

    public interface ITypeWrappedCypherFluentQuery<T>// where T : class, INeo4jNode, new()
    {
        ICypherFluentQuery internalQ { get; set; }
        ITypeWrappedCypherFluentQuery<T, T> rootQ { get; }
        ITransactionalGraphClient client { get; }
        string quearyName { get; }
        int PathCount { get; set; }
        List<ReadQueryParams<T>> readQueryParams { get; }
        List<PropertyInfo> pulledChildNodes { get; }

        ITypeWrappedCypherFluentQuery<T> AddIncludedChildNode(ReadQueryParams<T> readQueryParams);
    }
    public interface ITypeWrappedCypherFluentQuery<T, S> : ITypeWrappedCypherFluentQuery<T>// where T : class, INeo4jNode, new() //where S : class, INeo4jNode, new()
    {
        public IEnumerable<ReflectReadDictionary<T>> Results { get; }
        public int PathCount { get; set; }

        public ITypeWrappedCypherFluentQuery<T, S> Where(Expression<Func<T, bool>> delegat);
        public ITypeWrappedCypherFluentQuery<T, U> Include<U>(Expression<Func<T, U>> p);
        public ITypeWrappedCypherFluentQuery<T, U> ThenInclude<U>(Expression<Func<S, U>> p);

        public ITypeWrappedCypherFluentQuery<T, V> IncludeCollection<A, U, V>(Expression<Func<T, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        ;
        public ITypeWrappedCypherFluentQuery<T, V> ThenIncludeCollection<A, U, V>(Expression<Func<S, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        ;

        public ITypeWrappedCypherFluentQuery<T, U> IncludeCollection<U>(Expression<Func<T, IEnumerable<U>>> p) where U : class, INeo4jNode, new();
        public ITypeWrappedCypherFluentQuery<T, U> ThenIncludeCollection<U>(Expression<Func<S, IEnumerable<U>>> p) where U : class, INeo4jNode, new();
        public Expression<Func<IEnumerable<T>, IEnumerable<S>>> quearyExpression { get; }

        //Rebase

        //Single
        //First
        //Last
    }
    public class ColdTypeWrappedCypherFluentQuery<T, S> : ITypeWrappedCypherFluentQuery<T, S>// where T : class, INeo4jNode, new() //where S : class, INeo4jNode, new()
    {
        public static readonly Type t = typeof(T);
        public static readonly string name = typeof(T).QuerySaveName();
        public static readonly string labels = typeof(T).QuerySaveLabels();

        public ICypherFluentQuery internalQ { get; set; }
        public ITypeWrappedCypherFluentQuery<T, T> rootQ => rQ;
        private ColdTypeWrappedCypherFluentQuery<T, T> rQ { get; set; }
        public List<PropertyInfo> pulledChildNodes { get; set; } = new List<PropertyInfo>();
        public ITransactionalGraphClient client { get; }
        public List<ReadQueryParams<T>> readQueryParams { get; internal set; } = new List<ReadQueryParams<T>>();

        public string quearyName { get; } = "Rootobj";
        public Expression<Func<IEnumerable<T>, IEnumerable<S>>> quearyExpression { get; internal set; }

        public IEnumerable<ReflectReadDictionary<T>> Results => this.Return();

        public int CollectionDepth { get; private set; } = 0;
        public int PathCount { get; set; } = 0;

        public static ColdTypeWrappedCypherFluentQuery<U, U> Build<U>(ITransactionalGraphClient client) //where U : class, INeo4jNode, new()
        {
            ColdTypeWrappedCypherFluentQuery<U, U> ths = new ColdTypeWrappedCypherFluentQuery<U, U>(client);
            ths.rQ = ths;
            ths.quearyExpression = x => x;
            return ths;
        }
        private ColdTypeWrappedCypherFluentQuery<T, U> SubBuild<U>(ColdTypeWrappedCypherFluentQuery<T, S> source, string path, Expression<Func<IEnumerable<T>, IEnumerable<U>>> expression) //where U : class, INeo4jNode, new()
        {
            return new ColdTypeWrappedCypherFluentQuery<T, U>(source.internalQ, source.client, path)
            {
                PathCount = source.PathCount,
                pulledChildNodes = pulledChildNodes,
                readQueryParams = readQueryParams,
                quearyExpression = expression,
                rQ = rQ
            };
        }
        private ColdTypeWrappedCypherFluentQuery(ICypherFluentQuery internalQuery, ITransactionalGraphClient client, string path)
        {
            internalQ = internalQuery;
            this.client = client;
            this.quearyName = path;
        }
        private ColdTypeWrappedCypherFluentQuery(ITransactionalGraphClient client)
        {
            internalQ = client.Cypher.Match($"p0 = ({quearyName}:{labels})");
            PathCount++;
            this.client = client;
        }

        private IEnumerable<ReflectReadDictionary<T>> Return()
        {
            StringBuilder sb = new StringBuilder();

            //sb.Append("WITH ");
            for (int i = 0; i < PathCount; i++)
            {
                if (i > 0)
                {
                    sb.Append(" + ");
                }
                sb.Append($"collect(p{i})");
            }
            sb.Append($" AS paths{Environment.NewLine}");

            sb.Append($" UNWIND paths AS ret{Environment.NewLine}");
            sb.Append($" WITH DISTINCT ret AS dist{Environment.NewLine}");
            sb.Append($" WITH nodes(dist) + collect(labels(nodes(dist)[0])) + collect(labels(nodes(dist)[1])) + type(relationships(dist)[0]) AS meta{Environment.NewLine}");
            sb.Append($" with collect(meta) AS metas{Environment.NewLine}");
            //sb.Append($" return metas

            //sb.Append($"CALL apoc.convert.toTree(paths) yield value{Environment.NewLine}");
            //sb.Append($"WITH collect(value) as vals");
            //sb.Append("RETURN vals as json;");

            internalQ = internalQ.With(sb.ToString());
            var reter = internalQ.Return<string[][]>("metas as json");

            Dictionary<string, object> nodeHeap = new Dictionary<string, object>();
            var raw = reter.Results.Single()
                .AsParallel()
                .Select(x =>
                {
                    IDictionary<string, JToken?> parentJobj = JsonConvert.DeserializeObject<Dictionary<string, object>>(x[0])["data"] as JObject;
                    Type parentType = parentJobj.TryGetValue("__type__", out JToken? parentTypeStr) ?
                        ReflectionCache.BuildType(parentTypeStr.ToString(), typeof(INeo4jNode)) :
                        ReflectionCache.BuildType(JsonConvert.DeserializeObject<string[]>(x[2]), typeof(INeo4jNode));
                    object parentInstance = ManualDeserializeNode(
                        parentJobj.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value),
                        parentType,
                        nodeHeap
                    );

                    IDictionary<string, JToken?> childJobj = JsonConvert.DeserializeObject<Dictionary<string, object>>(x[1])["data"] as JObject;
                    Type childType = childJobj.TryGetValue("__type__", out JToken? childTypeStr) ?
                            ReflectionCache.BuildType(childTypeStr.ToString(), typeof(INeo4jNode)) :
                            ReflectionCache.BuildType(JsonConvert.DeserializeObject<string[]>(x[2]), typeof(INeo4jNode));
                    object childInstance = ManualDeserializeNode(
                        childJobj.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value),
                        childType,
                        nodeHeap
                    );

                    return ((INeo4jNode parentInstance, INeo4jNode childInstance, string relationship))(
                        (INeo4jNode)parentInstance,
                        (INeo4jNode)childInstance,
                        x[4]
                    );
                })
                .ToArray();

            Parallel.ForEach(raw, x =>
           {
               //merge objects
               ReflectionCache.Property pushProp =
                ReflectionCache.GetTypeData(x.parentInstance).props
                    .Where(y => y.Value.neo4JAttributes
                        .Where(z => z is ReferenceThroughRelationship)
                        .Cast<ReferenceThroughRelationship>()
                        .SingleOrDefault()
                        ?.Relationship
                        ?.ToLowerInvariant()
                        ==
                        x.Item3.ToLowerInvariant()
                        )
                    .Select(x => x.Value)
                    .Single()
                    ;

               object o = pushProp.PullValue(nodeHeap[x.parentInstance.Id]);
               Type colType = typeof(ICollection<>).MakeGenericType(x.childInstance.GetType());
               if (colType.IsAssignableFrom(pushProp.info.PropertyType))
               {
                   if(o == null)
                   {
                       o = Activator.CreateInstance(pushProp.info.PropertyType);
                   }
                   colType.GetMethods()
                    .Where(x => x.Name == "Add" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == o.GetType())
                    .Single()
                    .Invoke(o, new object[] { nodeHeap[x.childInstance.Id] });

                   pushProp.PushValue(nodeHeap[x.parentInstance.Id], o);
               }
               else
               {
                   pushProp.PushValue(nodeHeap[x.parentInstance.Id], nodeHeap[x.childInstance.Id]);
               }
           }
            );

            var ret = nodeHeap
                //remove child-only/non-root retreived values
                .Join(raw, x => x.Key, x => (x.Item1 as INeo4jNode)?.Id ?? "", (a, b) => a.Value as INeo4jNode)
                .Distinct()
                .Where(x => x != null && x is T)
                .Cast<T>()
                .Select(x => new ReflectReadDictionary<T>(x))
                .ToArray();

            return ret;
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

        //private IEnumerable<ReflectReadDictionary<T>> Return()s
        //{
        //    IEnumerable<ReadQueryParams<T>> rqp =
        //        rQ.BackCopy(this).readQueryParams
        //        .Distinct(new ReadQueryParams<T>())
        //        .Prepend(new ReadQueryParams<T>()
        //        {
        //            childName = rootQ.quearyName,
        //            Type = typeof(T)
        //        })
        //        .OrderBy(x => x.childName.Where(y => y == '_').Count());
        //    string qName = rootQ.quearyName;

        //    Type vr = RuntimeTypeBuilder
        //                .MyTypeBuilder
        //                .CompileResultTypeInfo(
        //                        rqp
        //                        .Select(x => x.IsCollection ?
        //                            new RuntimeTypeBuilder.FieldDescriptor(x.childName, typeof(IEnumerable<>).MakeGenericType(x.Type.GetGenericArguments()[0])) :
        //                            new RuntimeTypeBuilder.FieldDescriptor(x.childName, x.Type))
        //                        .ToList());

        //    ConstructorInfo cotr = vr.GetConstructors().First();

        //    ParameterExpression inQ = Expression.Parameter(typeof(ICypherFluentQuery), "internalQ");

        //    List<ParameterExpression> param = new List<ParameterExpression>();
        //    List<MemberInfo> mems = new List<MemberInfo>();
        //    List<Expression> asexps = new List<Expression>();

        //    var az0 = typeof(ICypherResultItem).GetMethods()
        //            .Where(x => x.Name.Contains("As") && x.ContainsGenericParameters && x.GetGenericArguments().Length == 1);
        //    var az = az0.OrderBy(x => x.Name.Length).First();

        //    ParameterExpression prm = Expression.Parameter(typeof(ICypherResultItem), qName);
        //    MemberInfo mem = vr.GetMember(qName).Single();
        //    Expression asexp = Expression.Call(prm, az.MakeGenericMethod(typeof(T)));

        //    foreach (ReadQueryParams<T> v in rqp)
        //    {
        //        prm = Expression.Parameter(typeof(ICypherResultItem), v.childName);
        //        param.Add(prm);

        //        mem = vr.GetMember(v.childName).Single();
        //        mems.Add(mem);

        //        if (v.IsCollection)
        //        {
        //            az = az0.OrderBy(x => x.Name.Length).Skip(1).First();
        //            asexp = Expression.Call(prm, az.MakeGenericMethod(v.Type.GetGenericArguments()[0]));
        //        }
        //        else
        //        {
        //            az = az0.OrderBy(x => x.Name.Length).First();
        //            asexp = Expression.Call(prm, az.MakeGenericMethod(v.Type));
        //        }

        //        asexps.Add(asexp);
        //    }

        //    Expression e0 =
        //        Expression.Lambda(
        //            Expression.New(
        //                cotr,
        //                asexps,
        //                mems),
        //            param
        //        )
        //    ;

        //    Type f = Expression.GetFuncType(param.Select(x => typeof(ICypherResultItem)).Append(vr).ToArray());

        //    IEnumerable<MethodInfo> allRetunr = typeof(ICypherFluentQuery)
        //        .GetMethods()
        //        .Where(x => x.Name == "Return" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.GetGenericArguments().Length > 0)
        //        .Where(x =>
        //        {
        //            Type pf = x.GetParameters()[0].ParameterType.GetGenericArguments()[0];
        //            return pf.GetGenericArguments().Length == f.GetGenericArguments().Length;
        //        })
        //        .ToArray();
        //    MethodInfo retunr = allRetunr
        //        .First()
        //        .MakeGenericMethod(vr);
        //    Expression retExprs = Expression.Call(
        //        inQ,
        //        retunr,
        //        e0
        //    );

        //    PropertyInfo vals = typeof(ICypherFluentQuery<>).MakeGenericType(vr).GetProperties().Where(x => x.Name.Contains("Results")).First();
        //    Expression values = Expression.Property(retExprs, vals.GetGetMethod());


        //    Type rrdvr = typeof(ReflectReadDictionary<>).MakeGenericType(vr);
        //    ConstructorInfo rrdvrCotr = rrdvr
        //        .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Public)
        //        .Where(x =>
        //            x.GetParameters().Length == 2)
        //        .First();

        //    var select = typeof(Enumerable).GetMethods().Where(x =>
        //        x.Name.Contains("Select") &&
        //        x.GetParameters().Length == 2 &&
        //        x.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2
        //    )
        //    .First();

        //    ParameterExpression sngl = Expression.Parameter(vr, "sngl");
        //    Expression wrapped = Expression.Call(
        //        null,
        //        select.MakeGenericMethod(vr, rrdvr),
        //        values,
        //        Expression.Lambda(
        //            Expression.New(rrdvrCotr, sngl, Expression.Constant(true)),
        //            sngl
        //        )
        //    );


        //    MethodInfo collapse = rrdvr
        //        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Public)
        //        .Where(x => x.Name.Contains("CollapseMappingObject") && x.GetParameters().Length == 0)
        //        .First();

        //    ParameterExpression snglrrd = Expression.Parameter(rrdvr, "snglrrd");
        //    Expression collapsed = Expression.Call(
        //        null,
        //        select.MakeGenericMethod(vr, typeof(ReflectReadDictionary<>).MakeGenericType(typeof(T))),
        //        values,
        //        Expression.Lambda(
        //            Expression.Call(
        //                Expression.New(rrdvrCotr, sngl, Expression.Constant(true)),
        //                collapse.MakeGenericMethod(typeof(T))
        //            ),
        //            sngl
        //        )
        //    );


        //    LambdaExpression l0 = Expression.Lambda(retExprs, inQ);
        //    Delegate d0 = l0.Compile();
        //    var q0 = d0.DynamicInvoke(internalQ);

        //    LambdaExpression l = Expression.Lambda(collapsed, inQ);
        //    Delegate d = l.Compile();
        //    var q = d.DynamicInvoke(internalQ) as IEnumerable<ReflectReadDictionary<T>>;


        //    return q.ToList();
        //}

        public ITypeWrappedCypherFluentQuery<T, S> Where(Expression<Func<T, bool>> delegat)
        {
            internalQ = internalQ.Where(Scrub(delegat));
            return this;
        }
        public ITypeWrappedCypherFluentQuery<T, U> Include<U>(Expression<Func<T, U>> p) //where U : class, INeo4jNode, new()
        {
            return rQ.BackCopy(this).ThenInclude(p);
        }

        public ITypeWrappedCypherFluentQuery<T, U> ThenInclude<U>(Expression<Func<S, U>> p) //where U : class, INeo4jNode, new()
        {
            p = Scrub(p);

            List<(string, ReflectionCache.Property, MemberExpression)> fullPath = new List<(string, ReflectionCache.Property, MemberExpression)>();

            Expression bdy = p.Body;
            ColdTypeWrappedCypherFluentQuery<T, S> ths = this;
            while (bdy is MemberExpression)
            {
                MemberExpression mExp = bdy as MemberExpression;
                PropertyInfo propInfo = mExp.Member as PropertyInfo;
                ReflectionCache.Property prop = new ReflectionCache.Property(propInfo, false);

                fullPath.Add((mExp.ToString().Replace('.', '_'), prop, mExp));

                bdy = mExp.Expression;
            }

            fullPath.Reverse();

            foreach (var v in fullPath)
            {
                string[] strPath = v.Item1.Split("_");
                string parentName = strPath.Take(strPath.Length - 1).Aggregate((a, b) => a + "_" + b);
                ths = ths.BackCopy
                    (v.Item2.ReadValue(new DB.ReadQueryParams<T>()
                    {
                        typeWrappedCypherFluentQuery = this,
                        childName = v.Item1,
                        parentName = parentName,
                        prop = v.Item2
                    }).typeWrappedCypherFluentQuery
                );
            }

            string ultimateChild = fullPath.Any() ? fullPath.Last().Item1 : "";

            MethodInfo select = typeof(Enumerable).GetMethods().Where(x => x.Name == "Select" && x.GetParameters().Length == 2 && x.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2).Single();
            Expression<Func<IEnumerable<T>, IEnumerable<U>>> combined =
                Expression.Lambda<Func<IEnumerable<T>, IEnumerable<U>>>(
                    Expression.Call(
                        null,
                        select.MakeGenericMethod(typeof(S), typeof(U)),
                        quearyExpression.Body,
                        Scrub(p)
                    ),
                quearyExpression.Parameters.First());

            return SubBuild<U>(this, ultimateChild, combined);
        }

        private ColdTypeWrappedCypherFluentQuery<T, S> BackCopy(ITypeWrappedCypherFluentQuery<T> typeWrappedCypherFluentQuery)
        {
            ColdTypeWrappedCypherFluentQuery<T, S> bccp = this;
            bccp.internalQ = typeWrappedCypherFluentQuery.internalQ;
            bccp.pulledChildNodes = bccp.pulledChildNodes.Union(
                typeWrappedCypherFluentQuery.pulledChildNodes
            ).Distinct().ToList();
            bccp.readQueryParams = bccp.readQueryParams.Union(
                typeWrappedCypherFluentQuery.readQueryParams
            ).Distinct().ToList();
            bccp.PathCount = PathCount;
            if (bccp is ColdTypeWrappedCypherFluentQuery<T, T>)
            {
                var v = (bccp as ColdTypeWrappedCypherFluentQuery<T, T>);
                v.quearyExpression = x => x;
                bccp = v as ColdTypeWrappedCypherFluentQuery<T, S>;
            }
            return bccp;
        }
        public ITypeWrappedCypherFluentQuery<T, V> IncludeCollection<A, U, V>(Expression<Func<T, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            return rQ.BackCopy(this).ThenIncludeCollection(p, q);
        }
        public ITypeWrappedCypherFluentQuery<T, V> ThenIncludeCollection<A, U, V>(Expression<Func<S, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            //if (this.CollectionDepth > 0)
            //{
            //    return CachedTypeWrappedCypherFluentQuery<T, S>.Build(this).ThenIncludeCollection(p, q);
            //}

            ColdTypeWrappedCypherFluentQuery<T, A> s1 = (ColdTypeWrappedCypherFluentQuery<T, A>)this.ThenInclude(p);

            MethodInfo select = typeof(Enumerable).GetMethods().Where(x => x.Name == "SelectMany" && x.GetParameters().Length == 2 && x.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2).Single();
            Expression<Func<IEnumerable<T>, IEnumerable<U>>> combined =
                Expression.Lambda<Func<IEnumerable<T>, IEnumerable<U>>>(
                    Expression.Call(
                        null,
                        select.MakeGenericMethod(typeof(S), typeof(U)),
                        quearyExpression.Body,
                        Scrub(p)
                    ),
                quearyExpression.Parameters.First());

            ColdTypeWrappedCypherFluentQuery<T, U> s2 = s1.SubBuild<U>(s1, s1.quearyName, combined);
            ColdTypeWrappedCypherFluentQuery<T, V> s3 = (ColdTypeWrappedCypherFluentQuery<T, V>)s2.ThenInclude(q);
            s3.CollectionDepth += this.CollectionDepth + 1;
            return s3;
        }

        public ITypeWrappedCypherFluentQuery<T, U> IncludeCollection<U>(Expression<Func<T, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        {
            return this.IncludeCollection<IEnumerable<U>, U, U>(p, x => x);
        }
        public ITypeWrappedCypherFluentQuery<T, U> ThenIncludeCollection<U>(Expression<Func<S, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        {
            return this.ThenIncludeCollection<IEnumerable<U>, U, U>(p, x => x);
        }


        public ITypeWrappedCypherFluentQuery<T> AddIncludedChildNode(ReadQueryParams<T> readQueryParams)
        {
            this.readQueryParams.Add(readQueryParams);
            return this;
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

    public class CachedTypeWrappedCypherFluentQuery<T, S> : ITypeWrappedCypherFluentQuery<T, S>// where T : class, INeo4jNode, new() //where S : class, INeo4jNode, new()
    {
        public ICypherFluentQuery internalQ { get; set; }
        public ITransactionalGraphClient client { get; }
        public List<ReadQueryParams<T>> readQueryParams => null;

        public ITypeWrappedCypherFluentQuery<T, T> rootQ => rQ;
        private CachedTypeWrappedCypherFluentQuery<T, T> rQ { get; set; }

        public string quearyName { get; } = "Rootobj";
        public Expression<Func<IEnumerable<T>, IEnumerable<S>>> quearyExpression { get; internal set; }

        private IEnumerable<T> _results { get; set; }
        public IEnumerable<ReflectReadDictionary<T>> Results => _results.Select(x => new ReflectReadDictionary<T>(x)).ToArray();

        public int CollectionDepth { get; private set; } = 0;

        public static CachedTypeWrappedCypherFluentQuery<T, S> Build(ITypeWrappedCypherFluentQuery<T, S> source) //where U : class, INeo4jNode, new()
        {
            CachedTypeWrappedCypherFluentQuery<T, S> ret = new CachedTypeWrappedCypherFluentQuery<T, S>(source.client)
            {
                _results = source.Results.Select(x => x.backingInstance).ToArray(),
                quearyExpression = source.quearyExpression
            };
            return ret;
        }
        private CachedTypeWrappedCypherFluentQuery(ITransactionalGraphClient client)
        {
            this.client = client;
        }

        public ITypeWrappedCypherFluentQuery<T, S> Where(Expression<Func<T, bool>> delegat)
        {
            throw new NotImplementedException();
        }
        public ITypeWrappedCypherFluentQuery<T, U> Include<U>(Expression<Func<T, U>> p) //where U : class, INeo4jNode, new()
        {
            throw new NotImplementedException();
        }

        public ITypeWrappedCypherFluentQuery<T, U> ThenInclude<U>(Expression<Func<S, U>> p) //where U : class, INeo4jNode, new()
        {
            throw new NotImplementedException();
        }

        public ITypeWrappedCypherFluentQuery<T, V> IncludeCollection<A, U, V>(Expression<Func<T, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            throw new NotImplementedException();
        }
        public ITypeWrappedCypherFluentQuery<T, V> ThenIncludeCollection<A, U, V>(Expression<Func<S, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            p = Scrub(p);
            q = Scrub(q);

            MethodInfo select = typeof(Enumerable).GetMethods().Where(x => x.Name == "SelectMany" && x.GetParameters().Length == 2 && x.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2).Single();
            Expression<Func<IEnumerable<T>, IEnumerable<U>>> combined =
                Expression.Lambda<Func<IEnumerable<T>, IEnumerable<U>>>(
                    Expression.Call(
                        null,
                        select.MakeGenericMethod(typeof(S), typeof(U)),
                        quearyExpression.Body,
                        p
                    ),
                quearyExpression.Parameters.First());

            //Expression<Func<IEnumerable<T>, IEnumerable<V>>> chained = x => 
            //    quearyExpression.Compile()(x).SelectMany(b => p.Compile()(b)).Select<U, V>(a => q.Compile()(a));

            Expression<Func<IEnumerable<T>, IEnumerable<S>>> locSwap = Expression.Lambda<Func<IEnumerable<T>, IEnumerable<S>>>(
                quearyExpression.Body,
                quearyExpression.Parameters.First()
            );
            Func<IEnumerable<T>, IEnumerable<S>> comp = locSwap.Compile();
            IEnumerable<S> localEnumerable = comp(_results).ToArray();

            foreach (S s in localEnumerable)
            {
                ReflectReadDictionary<S> ws = new ReflectReadDictionary<S>(s);

                A val = Neo4jSet<S>.All(client)
                    .Include(p).Results
                    .Where(x => x["Id"] == ws["Id"])
                    .Select(x => x.backingInstance)
                    .Select(x => p.Compile()(x))
                    .First();

                ws.Set(p, val);
            }

            throw new NotImplementedException();
        }

        public ITypeWrappedCypherFluentQuery<T, U> IncludeCollection<U>(Expression<Func<T, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        {
            return this.IncludeCollection<IEnumerable<U>, U, U>(p, x => x);
        }
        public ITypeWrappedCypherFluentQuery<T, U> ThenIncludeCollection<U>(Expression<Func<S, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        {
            return this.ThenIncludeCollection<IEnumerable<U>, U, U>(p, x => x);
        }

        public ITypeWrappedCypherFluentQuery<T> AddIncludedChildNode(ReadQueryParams<T> readQueryParams) => this;

        public List<PropertyInfo> pulledChildNodes => null;

        public int PathCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
