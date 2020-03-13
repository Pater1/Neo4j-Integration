using Neo4j.Driver;
using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models;
using Neo4jIntegration.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Neo4jIntegration.DB
{
    public class LiveDbObject<T> : IReadOnlyList<string>, IReadOnlyDictionary<string, object>
    {
        public static ReflectionCache.Type PropCache { get; } = ReflectionCache.GetTypeData(typeof(T));
        private static Dictionary<string, LiveDbObject<T>> livePool { get; } = new Dictionary<string, LiveDbObject<T>>();

        public T BackingInstance { get; }
        public Func<IDriver> GraphClientFactory { get; }
        public LiveObjectMode LiveMode { get; set; }

        public static implicit operator T(LiveDbObject<T> rrdt)
        {
            return rrdt.BackingInstance;
        }

        public static LiveDbObject<T> Build(T buildFor, Func<IDriver> graphClient, LiveObjectMode liveMode)
        {
            LiveDbObject<T> ret = null;
            if (buildFor is INeo4jNode)
            {
                INeo4jNode node = buildFor as INeo4jNode;

                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    node.Id = LiveDbObject<T>.PropCache.props["Id"].neo4JAttributes
                        .Where(x => x is IDAttribute)
                        .Cast<IDAttribute>()
                        .Select(x => x.GenerateId())
                        .Single();

                    ret = new LiveDbObject<T>(buildFor, graphClient, liveMode);
                    livePool.Add(node.Id, ret);
                    return ret;
                }
                else if (livePool.TryGetValue(node.Id, out ret))
                {
                    return ret;
                }
                else
                {
                    ret = new LiveDbObject<T>(buildFor, graphClient, liveMode);
                    livePool.Add(node.Id, ret);
                    return ret;

                }
            }
            return new LiveDbObject<T>(buildFor, graphClient, liveMode);
        }
        private LiveDbObject(T backingInstance, Func<IDriver> graphClientFactory, LiveObjectMode liveMode)
        {
            BackingInstance = backingInstance;
            GraphClientFactory = graphClientFactory;
            LiveMode = liveMode;
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

        public async Task Set<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            var member = (expression.Body as MemberExpression).Member as PropertyInfo;
            var prop = PropCache.props[member.Name];

            if ((LiveMode & LiveObjectMode.LiveWrite) != 0)
            {
                await DBOps.SaveValue(this, prop, value, GraphClientFactory);
            }
            else if ((LiveMode & LiveObjectMode.DeferedWrite) != 0)
            {
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    await DBOps.SaveValue(this, prop, value, GraphClientFactory);
                });
            }

            member.SetValue(BackingInstance, value);
        }
        public async Task<TValue> Get<TValue>(Expression<Func<T, TValue>> expression)
        {

            if ((LiveMode & LiveObjectMode.LiveRead) != 0)
            {
                IEnumerable<LiveDbObject<T>> results = (await Neo4jSet<T>.All(GraphClientFactory).Include(expression).ReturnAsync()).ToList();
                IEnumerable<LiveDbObject<T>> filtered = results.Where(x => x["Id"] == this["Id"]).ToList();
                TValue[] vals = await Task.WhenAll(filtered.Select(x => {
                    LiveObjectMode oldMode = x.LiveMode;
                    x.LiveMode = LiveObjectMode.Ignore;

                    var ret = x.Get(expression);

                    x.LiveMode = oldMode;

                    return ret;
                }));
                TValue remote = vals.SingleOrDefault();
                if (!remote.Equals(default))
                {
                    await Set(expression, remote);
                    return remote;
                }
            }
            else if ((LiveMode & LiveObjectMode.DeferedRead) != 0)
            {
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    TValue remote = Neo4jSet<T>.All(GraphClientFactory).Include(expression).Results.Where(x => x["Id"] == this["Id"]).Select(x => x.Get(expression)).Cast<TValue>().SingleOrDefault();
                    await Set(expression, remote);
                });
            }
            //TODO: get via CacheProperty to avoid recursion
            var member = (expression.Body as MemberExpression).Member as PropertyInfo;
            TValue local = (TValue)member.GetValue(BackingInstance);
            return local;
        }

        public async Task<TRet> Call<TObj, TValue, TRet>(Expression<Func<T, TObj>> selector, Func<TObj, TValue, TRet> call, TValue value) where TObj : INeo4jNode
        {
            TObj obj = await Get(selector);
            TRet ret = call(obj, value);

            if ((LiveMode & LiveObjectMode.LiveWrite) != 0)
            {
                await obj.Save(GraphClientFactory);
            }
            else if ((LiveMode & LiveObjectMode.DeferedWrite) != 0)
            {
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    await obj.Save(GraphClientFactory);
                });
            }

            return ret;
        }
        public async Task Call<TObj, TValue>(Expression<Func<T, TObj>> selector, Action<TObj, TValue> call, TValue value) where TObj : INeo4jNode
        {
            TObj obj = await Get(selector);
            call(obj, value);

            if ((LiveMode & LiveObjectMode.LiveWrite) != 0)
            {
                await DBOps.SaveNode<T>(this, GraphClientFactory);
            }
            else if ((LiveMode & LiveObjectMode.DeferedWrite) != 0)
            {
                ThreadPool.QueueUserWorkItem(async _ =>
                {
                    await obj.Save(GraphClientFactory);
                });
            }
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
