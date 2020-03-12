using Neo4jClient.Transactions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Neo4jIntegration.Reflection
{
    public class LiveDbObject<T> : IReadOnlyList<string>, IReadOnlyDictionary<string, object>
    {
        public static ReflectionCache.Type PropCache { get; } = ReflectionCache.GetTypeData(typeof(T));
        public T BackingInstance { get; }
        public Func<ITransactionalGraphClient> GraphClientFactory { get; }

        public static implicit operator T(LiveDbObject<T> rrdt)
        {
            return rrdt.BackingInstance;
        }

        public LiveDbObject(T backingInstance, Func<ITransactionalGraphClient> graphClientFactory)
        {
            BackingInstance = backingInstance;
            GraphClientFactory = graphClientFactory;
        }

        public LiveDbObject<T> Exclude(IEnumerable<string> excludes)
        {
            //var ret = new ReflectReadDictionary<T>(this);
            //ret.propCache.props =
            //        ret.propCache.props
            //            .Where(x => !excludes.Contains(x.Key))
            //            .ToDictionary(x => x.Key, x => x.Value);
            return this;
        }
        public LiveDbObject<T> Include(IEnumerable<string> includes)
        {
            //var ret = new ReflectReadDictionary<T>(this);
            //ret.propCache.props =
            //        ret.propCache.props
            //            .Where(x => includes.Contains(x.Key))
            //            .ToDictionary(x => x.Key, x => x.Value);
            return this;
        }

        public LiveDbObject<T> SetChainable<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            Set(expression, value);
            return this;
        }
        public void Set<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            var member = (expression.Body as MemberExpression).Member as PropertyInfo;

            var rt = PropCache.props[member.Name];
            PropCache.props[member.Name] = rt;

            member.SetValue(BackingInstance, value);
        }
        public LiveDbObject<T> GetChainable<TValue>(Expression<Func<T, TValue>> expression, out TValue value)
        {
            value = Get(expression);
            return this;
        }
        public TValue Get<TValue>(Expression<Func<T, TValue>> expression)
        {
            var member = (expression.Body as MemberExpression).Member as PropertyInfo;

            return (TValue)member.GetValue(BackingInstance);
        }

        public string this[int index] => PropCache.PropNames.ElementAt(index);
        public object this[string key]
        {
            get
            {
                return PropCache.props[key].PullValue(BackingInstance);
            }
            set
            {
                PropCache.props[key].PushValue(BackingInstance, value);
            }
        }

        public int Count => PropCache.props.Count();

        public IEnumerable<string> Keys => PropCache.PropNames;

        public IEnumerable<object> Values
        {
            get
            {
                T bi = BackingInstance;
                return PropCache.props.Select(x => x.Value.PullValue(bi));
            }
        }

        public bool ContainsKey(string key)
        {
            return PropCache.PropNames.Contains(key);
        }

        public IEnumerator<string> GetEnumerator() => Keys.GetEnumerator();

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            bool success = ContainsKey(key);
            value = success ? this[key] : null;
            return success;
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => ((IReadOnlyDictionary<string, object>)this).GetEnumerator();
        
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
            => ((IReadOnlyDictionary<string, object>)this).GetEnumerator();

        public bool Contains(KeyValuePair<string, object> item) => this.Contains(item.Key);
    }

}
