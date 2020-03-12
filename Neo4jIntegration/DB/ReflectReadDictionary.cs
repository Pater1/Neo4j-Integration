using Neo4jClient;
using Neo4jClient.Cypher;
using Neo4jClient.Transactions;
using Neo4jIntegration.Attributes;
using Neo4jIntegration.Models;
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
using Neo4jIntegration.DB;

namespace Neo4jIntegration.Reflection
{
    public static class ReflectReadDictionary
    {
        public static readonly CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
        public static readonly TextInfo textInfo = cultureInfo.TextInfo;
        public static bool Save<T>(this ReflectReadDictionary<T> buildFor, ITransactionalGraphClient client) where T : class, INeo4jNode, new()
        {
            //DependencyInjector depInj = DependencyInjector.Custom().Insert("GraphClient", client).Insert("ReflectRead", buildFor);
            //SaveQuearyParams<T> r = new SaveQuearyParams<T>()
            //{
            //    buildFor = buildFor,
            //    queary = client.Cypher,
            //    depInj = depInj,
            //    recursionDepth = 0
            //};
            //r = r.Save();
            //return r.success;

            DBOps<T>.SaveNode(buildFor, client);
            return true;
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

        public object backingObject { get => backingInstance; }
        public readonly T backingInstance;
        public ReflectionCache.Type propCache;
        public ReflectionCache.Property IDProp => propCache.ID;
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

        internal ReflectReadDictionary<T> SetChainable<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            Set(expression, value);
            return this;
        }
        public void Set<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            var member = (expression.Body as MemberExpression).Member as PropertyInfo;

            var rt = propCache.props[member.Name.ToLowerInvariant()];
            rt.WrittenTo = true;
            propCache.props[member.Name] = rt;

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
                return propCache.props[key.ToLower()].PullValue(backingInstance);
            }
            set
            {
                propCache.props[key.ToLower()].PushValue(backingInstance, value);
            }
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
