using Neo4jClient;
using Neo4jClient.Cypher;
using Neo4jClient.Transactions;
using plm_common.Attributes;
using plm_common.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using plm_common.DB;

namespace plm_common.Reflection
{
    public static class ReflectReadDictionary
    {
        public static readonly CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
        public static readonly TextInfo textInfo = cultureInfo.TextInfo;
        public static bool Save<T>(this ReflectReadDictionary<T> buildFor, ITransactionalGraphClient client) where T : class, INeo4jNode, new()
        {
            DependencyInjector depInj = DependencyInjector.Custom().Insert("GraphClient", client).Insert("ReflectRead", buildFor);
            SaveQuearyParams<T> r = new SaveQuearyParams<T>()
            {
                buildFor = buildFor,
                queary = client.Cypher,
                depInj = depInj,
                recursionDepth = 0
            };
            r = r.Save();
            return r.success;
        }
        
        public static ReflectReadDictionary<T> Save<T>(this T buildFor, ITransactionalGraphClient graphClient) where T : class, INeo4jNode, new()
        {
            ReflectReadDictionary<T> ret = buildFor.Build();
            ret.Save(graphClient);
            return ret;
        }

        public static ReflectReadDictionary<T> Build<T>(this T buildFor) where T : class, INeo4jNode, new()
        {
            return new ReflectReadDictionary<T>(buildFor);
        }
    }
    
    public interface IObjectBacked
    {
        object backingObject { get; }
    }
    public class ReflectReadDictionary<T> : IObjectBacked, IReadOnlyList<string>, IReadOnlyDictionary<string, object>, IDictionary<string, object> //where T : class, new()
    {
        private static ReflectionCache.Type s_PropCache
             = ((Func<ReflectionCache.Type>)(() => new ReflectionCache.Type(typeof(T), false)))();

        private ReflectReadDictionary<U> CollapseMappingObject<U>()// where U : class, new()
        {
            IEnumerable<(string mappedProp, string thisObj, IEnumerable<string> childPath, ReflectionCache.Property Value)> singleLayer = propCache.props.Select(x => (x.Key, x.Key.Split("_")[0], x.Key.Split("_").Skip(1), x.Value));
            U rootInst = (U)this[singleLayer.Select(x => x.thisObj).Distinct().Single()];
            return CollapseMappingObject(this, rootInst, singleLayer.Where(x => x.childPath.Count() > 0).Select(x => (x.mappedProp, x.childPath.First(), x.childPath.Skip(1), x.Value)));
        }
        private static ReflectReadDictionary<U> CollapseMappingObject<U,V>(ReflectReadDictionary<V> map, U bi, IEnumerable<(string mappedProp, string directChild, IEnumerable<string> childPath, ReflectionCache.Property Value)> singleLayer) //where U : class, new() where V : class, new()
        {
            ReflectReadDictionary<U> ret = new ReflectReadDictionary<U>(bi);
            foreach(
                (string mappedProp, string directChild) 
                    in 
                singleLayer
                    .Where(x => x.childPath.Count() == 0)
                    .Select(x => (x.mappedProp, x.directChild)).Distinct()
            ){
                object chl = map[mappedProp];

                if (chl != null)
                {
                    if (chl.GetType().IsEnumerable())
                    {
                        chl = ChainFillCollection(map, (IEnumerable)chl, directChild, singleLayer);
                    }
                    else
                    {
                        chl = ChainFillObject(map, chl, directChild, singleLayer);
                    }
                }

                map[mappedProp] = chl;
                ret[directChild] = chl;
            }
            return ret;
        }
        private static object ChainFillCollection<V>(ReflectReadDictionary<V> map, IEnumerable chl, string directChild, IEnumerable<(string mappedProp, string directChild, IEnumerable<string> childPath, ReflectionCache.Property Value)> singleLayer)
        {
            Type t = chl.GetType().GetGenericArguments()[0];
            Type refColT = typeof(ReferenceCollection<>).MakeGenericType(t);
            ConstructorInfo refColCotr = refColT.GetConstructors().Where(x => x.GetParameters().Length == 0).Single();

            Delegate newRefCol = Expression.Lambda(
                Expression.New(refColCotr),
                new ParameterExpression[0]
            ).Compile();

            object refCol = newRefCol.DynamicInvoke();

            ParameterExpression refColParam = Expression.Parameter(refColT, "refCol");
            ParameterExpression refColObj = Expression.Parameter(t, "obj");

            MethodInfo add = refColT.GetMethods().Where(x => x.GetParameters().Length == 1 && x.Name.Contains("Add")).First();
            Delegate doAdd = Expression.Lambda(
                Expression.Call(
                    refColParam,
                    add,
                    refColObj
                ),
                refColParam,
                refColObj
            ).Compile();

            foreach(object o in chl)
            {
                object obj = ChainFillObject(map, o, directChild, singleLayer);
                doAdd.DynamicInvoke(refCol, obj);
            }

            MethodInfo dedup = refColT.GetMethods().Where(x => x.GetParameters().Length == 0 && x.Name.Contains("DeDup")).First();
            Delegate doDedup = Expression.Lambda(
                Expression.Call(
                    refColParam,
                    dedup
                ),
                refColParam
            ).Compile();
            doDedup.DynamicInvoke(refCol);

            return refCol;
        }
        private static object ChainFillObject<V>(ReflectReadDictionary<V> map, object chl, string directChild, IEnumerable<(string mappedProp, string directChild, IEnumerable<string> childPath, ReflectionCache.Property Value)> singleLayer) { 
            //TODO if chl is collection, add all chl to ReferenceCollection
            Type tmap = typeof(V);
            Type rrtmap = typeof(ReflectReadDictionary<V>);

            Type tchl = chl.GetType();
            Type rrchl = typeof(ReflectReadDictionary<>).MakeGenericType(tchl);
            MethodInfo mthchl = rrchl
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Single(x => x.Name == nameof(map.CollapseMappingObject) && x.GetParameters().Length == 3)
                .MakeGenericMethod(tchl, tmap);
            ConstructorInfo cotr = rrchl.GetConstructor(new Type[] { tchl, typeof(bool) });

            ParameterExpression rrtmapara = Expression.Parameter(rrtmap, "rrtmap");
            ParameterExpression child = Expression.Parameter(tchl, "child");
            ParameterExpression layer = Expression.Parameter(singleLayer.GetType(), "layer");
            LambdaExpression lmbd = Expression.Lambda(
                Expression.Call(
                    null,
                    mthchl,
                    rrtmapara,
                    child,
                    layer
                ),
                rrtmapara,
                child,
                layer
            );
            Delegate comp = lmbd.Compile();

            var subLayer = singleLayer.Where(x => x.childPath.Count() > 0 && x.directChild == directChild).Select(x => (x.mappedProp, x.childPath.First(), x.childPath.Skip(1), x.Value));
            IObjectBacked chlrr = (IObjectBacked)comp.DynamicInvoke(map, chl, subLayer);
            return chlrr.backingObject;
        }

        public object backingObject { get => backingInstance; }
        public readonly T backingInstance;
        public ReflectionCache.Type propCache;
        public ReflectionCache.Property IDProp => propCache.ID;
        public string StrID => IDProp.WriteValidate(DependencyInjector.Custom(), backingInstance).ToString();
        public ReflectReadDictionary(T backingInstance)
        {
            this.backingInstance = backingInstance;
            this.propCache = ((Func<ReflectionCache.Type>)(() => new ReflectionCache.Type(typeof(T), false)))();
        }
        private ReflectReadDictionary(T backingInstance, bool noCheck)
        {
            this.backingInstance = backingInstance;
            this.propCache = ((Func<ReflectionCache.Type>)(() => new ReflectionCache.Type(typeof(T), noCheck)))();
        }
        private ReflectReadDictionary(ReflectReadDictionary<T> clone)
        {
            this.backingInstance = clone.backingInstance;
            this.propCache = s_PropCache.DeepClone();
        }

        public ReflectReadDictionary<T> Exclude(IEnumerable<string> excludes)
        {
            var ret = new ReflectReadDictionary<T>(this);
            ret.propCache.props =
                    ret.propCache.props
                        .Where(x => !excludes.Contains(x.Key))
                        .ToDictionary(x => x.Key, x => x.Value);
            return ret;
        }
        public ReflectReadDictionary<T> Include(IEnumerable<string> includes)
        {
            var ret = new ReflectReadDictionary<T>(this);
            ret.propCache.props =
                    ret.propCache.props
                        .Where(x => includes.Contains(x.Key))
                        .ToDictionary(x => x.Key, x => x.Value);
            return ret;
        }

        public string PullValidObjectID(DependencyInjector dependencyInjector)
        {
            return propCache.ID.WriteValidate(dependencyInjector, backingInstance).ToString();
        }


        internal ReflectReadDictionary<T> SetChainable<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            Set(expression, value);
            return this;
        }
        public void Set<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            var member = (expression.Body as MemberExpression).Member as PropertyInfo;

            var rt = propCache.propsInfoFirst[member];
            rt.WrittenTo = true;
            propCache.propsInfoFirst[member] = rt;

            member.SetValue(backingInstance, value);
        }
        public TValue Get<TValue>(Expression<Func<T, TValue>> expression)
        {
            var member = (expression.Body as MemberExpression).Member as PropertyInfo;

            return (TValue)member.GetValue(backingInstance);
        }


        public string this[int index] => propCache.PropNames.ElementAt(index);
        public object this[string key]
        {
            get
            {
                return propCache.props[ReflectReadDictionary.textInfo.ToTitleCase(key)].PullValue(backingInstance);
            }
            set
            {
                propCache.props[ReflectReadDictionary.textInfo.ToTitleCase(key)].PushValue(backingInstance, value);
            }
        }
        public void WriteValidate(DependencyInjector depInj)
        {
            T bi = backingInstance;
            Parallel.ForEach(propCache.props, x =>
            {
                x.Value.WriteValidate(depInj.Clone(), bi);
            });
        }

        public int Count => propCache.props.Count();

        public IEnumerable<string> Keys => propCache.PropNames;

        public IEnumerable<object> Values
        {
            get
            {
                T bi = backingInstance;
                return s_PropCache.props.Select(x => x.Value.PullValue(bi));
            }
        }

        ICollection<string> IDictionary<string, object>.Keys => throw new NotImplementedException();

        ICollection<object> IDictionary<string, object>.Values => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public bool ContainsKey(string key)
        {
            return s_PropCache.PropNames.Contains(key);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Keys.GetEnumerator();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            bool success = ContainsKey(key);
            value = success ? this[key] : null;
            return success;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        internal string InLineJson()
        {
            throw new NotImplementedException();
        }

        public void Add(string key, object value)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }
    }

}
