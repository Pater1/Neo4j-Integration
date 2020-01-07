using Neo4j.Driver.V1;
using Neo4jClient;
using Neo4jClient.Cypher;
using Neo4jClient.Transactions;
using plm_common.DB;
using plm_common.Extentions;
using plm_common.Models;
using plm_common.Reflection;
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

namespace plm_common
{
    public sealed class Neo4jSet<T> where T : class, INeo4jNode, ITemplatable<T>, new()
    {
        public static readonly string setName = typeof(T).QuerySaveName();

        public static ReflectReadDictionary<T> SingleValue(string id, ITransactionalGraphClient client)
            => Single(id, client).Results.FirstOrDefault();
        public static IEnumerable<ReflectReadDictionary<T>> AllValue(ITransactionalGraphClient client)
            => All(client).Results;

        public static TypeWrappedCypherFluentQuery<T, T> Single(string id, ITransactionalGraphClient client)
            => All(client).Where(x => x.Id == id);
        public static TypeWrappedCypherFluentQuery<T, T> All(ITransactionalGraphClient client)
        {
            return TypeWrappedCypherFluentQuery<T, T>.Build<T>(client);
        }
    }

    public interface ITypeWrappedCypherFluentQuery<T>// where T : class, INeo4jNode, new()
    {
        ICypherFluentQuery internalQ { get; set; }
        TypeWrappedCypherFluentQuery<T, T> rootQ { get; }
        List<PropertyInfo> pulledChildNodes { get; }
        ITransactionalGraphClient client { get; }
        string quearyName { get; }
        List<ReadQueryParams<T>> readQueryParams { get; }

        ITypeWrappedCypherFluentQuery<T> AddIncludedChildNode(ReadQueryParams<T> readQueryParams);
    }
    public class TypeWrappedCypherFluentQuery<T, S> : ITypeWrappedCypherFluentQuery<T>// where T : class, INeo4jNode, new() //where S : class, INeo4jNode, new()
    {
        public static readonly Type t = typeof(T);
        public static readonly string name = typeof(T).QuerySaveName();
        public static readonly string labels = typeof(T).QuerySaveLabels();

        public ICypherFluentQuery internalQ { get; set; }
        public TypeWrappedCypherFluentQuery<T, T> rootQ { get; set; }
        public List<PropertyInfo> pulledChildNodes { get; set; } = new List<PropertyInfo>();
        public ITransactionalGraphClient client { get; }
        public List<ReadQueryParams<T>> readQueryParams { get; internal set; } = new List<ReadQueryParams<T>>();

        public string quearyName { get; } = "Rootobj";

        public IEnumerable<ReflectReadDictionary<T>> Results => this.Return();

        public int CollectionDepth { get; private set; } = 0;

        public static TypeWrappedCypherFluentQuery<U, U> Build<U>(ITransactionalGraphClient client) //where U : class, INeo4jNode, new()
        {
            TypeWrappedCypherFluentQuery<U, U> ths = new TypeWrappedCypherFluentQuery<U, U>(client);
            ths.rootQ = ths;
            return ths;
        }
        private TypeWrappedCypherFluentQuery<T, U> SubBuild<U>(TypeWrappedCypherFluentQuery<T, S> source, string path) //where U : class, INeo4jNode, new()
        {
            return new TypeWrappedCypherFluentQuery<T, U>(source.internalQ, source.client, path)
            {
                pulledChildNodes = pulledChildNodes,
                readQueryParams = readQueryParams,
                rootQ = rootQ
            };
        }
        private TypeWrappedCypherFluentQuery(ICypherFluentQuery internalQuery, ITransactionalGraphClient client, string path)
        {
            internalQ = internalQuery;
            this.client = client;
            this.quearyName = path;
        }
        private TypeWrappedCypherFluentQuery(ITransactionalGraphClient client)
        {
            internalQ = client.Cypher.Match($"({quearyName}:{labels})");
            this.client = client;
        }

        public IEnumerable<ReflectReadDictionary<T>> Return()
        {
            IEnumerable<ReadQueryParams<T>> rqp =
                rootQ.BackCopy(this).readQueryParams
                .Distinct(new ReadQueryParams<T>())
                .Prepend(new ReadQueryParams<T>()
                {
                    childName = rootQ.quearyName,
                    Type = typeof(T)
                })
                .OrderBy(x => x.childName.Where(y => y == '_').Count());
            string qName = rootQ.quearyName;

            Type vr = RuntimeTypeBuilder
                        .MyTypeBuilder
                        .CompileResultTypeInfo(
                                rqp
                                .Select(x => x.IsCollection?
                                    new RuntimeTypeBuilder.FieldDescriptor(x.childName, typeof(IEnumerable<>).MakeGenericType(x.Type.GetGenericArguments()[0])):
                                    new RuntimeTypeBuilder.FieldDescriptor(x.childName, x.Type))
                                .ToList());

            ConstructorInfo cotr = vr.GetConstructors().First();

            ParameterExpression inQ = Expression.Parameter(typeof(ICypherFluentQuery), "internalQ");

            List<ParameterExpression> param = new List<ParameterExpression>();
            List<MemberInfo> mems = new List<MemberInfo>();
            List<Expression> asexps = new List<Expression>();

            var az0 = typeof(ICypherResultItem).GetMethods()
                    .Where(x => x.Name.Contains("As") && x.ContainsGenericParameters && x.GetGenericArguments().Length == 1);
            var az = az0.OrderBy(x => x.Name.Length).First();

            ParameterExpression prm = Expression.Parameter(typeof(ICypherResultItem), qName);
            MemberInfo mem = vr.GetMember(qName).Single();
            Expression asexp = Expression.Call(prm, az.MakeGenericMethod(typeof(T)));

            foreach (ReadQueryParams<T> v in rqp)
            {
                prm = Expression.Parameter(typeof(ICypherResultItem), v.childName);
                param.Add(prm);

                mem = vr.GetMember(v.childName).Single();
                mems.Add(mem);

                if (v.IsCollection)
                {
                    az = az0.OrderBy(x => x.Name.Length).Skip(1).First();
                    asexp = Expression.Call(prm, az.MakeGenericMethod(v.Type.GetGenericArguments()[0]));
                }
                else
                {
                    az = az0.OrderBy(x => x.Name.Length).First();
                    asexp = Expression.Call(prm, az.MakeGenericMethod(v.Type));
                }

                asexps.Add(asexp);
            }

            Expression e0 =
                Expression.Lambda(
                    Expression.New(
                        cotr,
                        asexps,
                        mems),
                    param
                )
            ;

            Type f = Expression.GetFuncType(param.Select(x => typeof(ICypherResultItem)).Append(vr).ToArray());

            IEnumerable<MethodInfo> allRetunr = typeof(ICypherFluentQuery)
                .GetMethods()
                .Where(x => x.Name == "Return" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType.GetGenericArguments().Length > 0)
                .Where(x =>
                {
                    Type pf = x.GetParameters()[0].ParameterType.GetGenericArguments()[0];
                    return pf.GetGenericArguments().Length == f.GetGenericArguments().Length;
                })
                .ToArray();
            MethodInfo retunr = allRetunr
                .First()
                .MakeGenericMethod(vr);
            Expression retExprs = Expression.Call(
                inQ,
                retunr,
                e0
            );

            PropertyInfo vals = typeof(ICypherFluentQuery<>).MakeGenericType(vr).GetProperties().Where(x => x.Name.Contains("Results")).First();
            Expression values = Expression.Property(retExprs, vals.GetGetMethod());


            Type rrdvr = typeof(ReflectReadDictionary<>).MakeGenericType(vr);
            ConstructorInfo rrdvrCotr = rrdvr
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Public)
                .Where(x =>
                    x.GetParameters().Length == 2)
                .First();

            var select = typeof(Enumerable).GetMethods().Where(x =>
                x.Name.Contains("Select") &&
                x.GetParameters().Length == 2 &&
                x.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2
            )
            .First();

            ParameterExpression sngl = Expression.Parameter(vr, "sngl");
            Expression wrapped = Expression.Call(
                null,
                select.MakeGenericMethod(vr, rrdvr),
                values,
                Expression.Lambda(
                    Expression.New(rrdvrCotr, sngl, Expression.Constant(true)),
                    sngl
                )
            );


            MethodInfo collapse = rrdvr
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Public)
                .Where(x => x.Name.Contains("CollapseMappingObject") && x.GetParameters().Length == 0)
                .First();

            ParameterExpression snglrrd = Expression.Parameter(rrdvr, "snglrrd");
            Expression collapsed = Expression.Call(
                null,
                select.MakeGenericMethod(vr, typeof(ReflectReadDictionary<>).MakeGenericType(typeof(T))),
                values,
                Expression.Lambda(
                    Expression.Call(
                        Expression.New(rrdvrCotr, sngl, Expression.Constant(true)),
                        collapse.MakeGenericMethod(typeof(T))
                    ),
                    sngl
                )
            );


            LambdaExpression l0 = Expression.Lambda(retExprs, inQ);
            Delegate d0 = l0.Compile();
            var q0 = d0.DynamicInvoke(internalQ);

            LambdaExpression l = Expression.Lambda(collapsed, inQ);
            Delegate d = l.Compile();
            var q = d.DynamicInvoke(internalQ) as IEnumerable<ReflectReadDictionary<T>>;


            return q.ToList();
        }
        public TypeWrappedCypherFluentQuery<T, S> Where(Expression<Func<T, bool>> delegat)
        {
            internalQ = internalQ.Where(Scrub(delegat));
            return this;
        }
        public TypeWrappedCypherFluentQuery<T, U> Include<U>(Expression<Func<T, U>> p) //where U : class, INeo4jNode, new()
        {
            return rootQ.BackCopy(this).ThenInclude(p);
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

            string ultimateChild = fullPath.Any()? fullPath.Last().Item1: "";

            return SubBuild<U>(this, ultimateChild);
        }

        private TypeWrappedCypherFluentQuery<T, S> BackCopy(ITypeWrappedCypherFluentQuery<T> typeWrappedCypherFluentQuery)
        {
            TypeWrappedCypherFluentQuery<T, S> bccp = this;
            bccp.internalQ = typeWrappedCypherFluentQuery.internalQ;
            bccp.pulledChildNodes = bccp.pulledChildNodes.Union(
                typeWrappedCypherFluentQuery.pulledChildNodes
            ).Distinct().ToList();
            bccp.readQueryParams = bccp.readQueryParams.Union(
                typeWrappedCypherFluentQuery.readQueryParams
            ).Distinct().ToList();
            return bccp;
        }
        public TypeWrappedCypherFluentQuery<T, V> IncludeCollection<A, U, V>(Expression<Func<T, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            return rootQ.BackCopy(this).ThenIncludeCollection(p, q);
        }
        public TypeWrappedCypherFluentQuery<T, V> ThenIncludeCollection<A, U, V>(Expression<Func<S, A>> p, Expression<Func<U, V>> q)
            where A : IEnumerable<U>
            //where U : class, INeo4jNode, new()
            //where V : class, INeo4jNode, new()
        {
            if(this.CollectionDepth > 0)
            {
                throw new NotSupportedException("Querying for collections containing collections is not yet supported! Please process these as seperate queries for now");
            }

            var s1 = this.ThenInclude(p);
            var s2 = s1.SubBuild<U>(s1, s1.quearyName);
            var s3 = s2.ThenInclude(q);
            s3.CollectionDepth += this.CollectionDepth+1;
            return s3;
        }

        //public TypeWrappedCypherFluentQuery<T, U> Include<U>(Expression<Func<S, IEnumerable<U>>> p) where U : class, INeo4jNode, new()
        //{
        //    p = Scrub(p);

        //    Expression bdy = p.Body;

        //    return null;
        //}


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
}
