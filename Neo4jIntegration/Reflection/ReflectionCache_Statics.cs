using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo4jIntegration.Reflection
{
    public partial class ReflectionCache
    {
        public static readonly Dictionary<string, System.Type[]> typeCache = new Dictionary<string, System.Type[]>();
        public static System.Type BuildType(string[] strs, System.Type assignableFilter = null)
        {
            string cacheKeye = strs.Select(x => x.GetHashCode()).Aggregate((a,b) =>
                {
                    unchecked
                    {
                        return a + b;
                    }
                }).ToString();

            if(typeCache.TryGetValue(cacheKeye, out System.Type[] ret))
            {
                IEnumerable<System.Type> ts = ret;
                if (ts.Count() > 1)
                {
                    ts = ts
                        .Where(x => assignableFilter == null || assignableFilter.IsAssignableFrom(x));
                }
                return ts.Single();
            }

            cacheKeye = string.Intern(cacheKeye);
            lock (cacheKeye)
            {
                List<(System.Type, int, bool)> ts = strs.Select(x =>
                {
                    System.Type t = BuildType(x, assignableFilter);

                    System.Type[] genArgs = t.GetGenericArguments();
                    (System.Type, int, bool) sngl = (t, genArgs.Length, genArgs.Length == 0);

                    return sngl;
                }).ToList();
                List<(System.Type, int, bool)> loc = new List<(System.Type, int, bool)>();
                while (ts.Count > 1)
                {
                    (System.Type, int, bool) genFil = ts[0];
                    bool error = true;
                    for (int i = 1; i < ts.Count; i++)
                    {
                        if (!genFil.Item3 && genFil.Item2 > 0 && genFil.Item2 <= loc.Count)
                        {
                            error = false;
                            break;
                        }
                        else if (ts[i].Item2 > 0 && !ts[i].Item3)
                        {
                            loc.Clear();
                            genFil = ts[i];
                        }
                        else
                        {
                            loc.Add(ts[i]);
                        }
                    }
                    if (!error || (!genFil.Item3 && genFil.Item2 > 0 && genFil.Item2 <= loc.Count))
                    {
                        foreach (var v in loc)
                        {
                            ts.Remove(v);
                        }

                        System.Type genFilled = genFil.Item1.MakeGenericType(
                            loc.Select(x => x.Item1).ToArray()
                        );

                        System.Type[] genArgs = genFilled.GetGenericArguments();
                        (System.Type, int, bool) sngl = (genFilled, genArgs.Length, true);

                        int pntr = ts.IndexOf(genFil);
                        ts[pntr] = sngl;
                    }
                    else
                    {

                    }
                }
                System.Type tr = ts.Select(x => x.Item1).Single();
                if (!typeCache.ContainsKey(cacheKeye))
                {
                    typeCache.Add(cacheKeye, new System.Type[] { tr });
                }
                return tr;
            }
        }
        public static System.Type BuildType(string str, System.Type assignableFilter = null)
        {
            System.Type fastRet = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(str)).Where(x => x != null).SingleOrDefault();

            if(fastRet != null)
            {
                return fastRet;
            }

            lock (typeCache)
            {
                if (!typeCache.ContainsKey(str) || !typeCache[str].Any())
                {
                    if (str.Contains("__"))
                    {
                        typeCache.Add(str, new System.Type[] { BuildType(str.Split("__"), assignableFilter) });
                    }
                    else
                    {
                        typeCache.Add(str,
                            AppDomain.CurrentDomain.GetAssemblies().SelectMany(x =>
                            {
                                try
                                {
                                    return x.GetTypes();
                                }
                                catch
                                {
                                    return new System.Type[0];
                                }
                            })
                            .Where(x =>
                            {
                                if (x.GetGenericArguments().Length > 0)
                                {

                                }
                                return x.Name.Split("`")[0] == str;
                            }
                            ).ToArray()
                        );
                    }
                }
            }

            IEnumerable<System.Type> ret = typeCache[str];
            if(ret.Count() > 1)
            {
                ret = ret
                    .Where(x => assignableFilter == null || assignableFilter.IsAssignableFrom(x));
            }
            return ret.Single();
        }

        public static ReflectionCache.Type GetTypeData<T>()
        {
            return GetTypeData(typeof(T));
        }
        public static ReflectionCache.Type GetTypeData(object o)
        {
            return GetTypeData(o.GetType());
        }
        private static readonly Dictionary<System.Type, ReflectionCache.Type> TypeDataCache = new Dictionary<System.Type, ReflectionCache.Type>();
        public static ReflectionCache.Type GetTypeData(System.Type t)
        {
            lock (TypeDataCache)
            {
                if (TypeDataCache.ContainsKey(t))
                {
                    return TypeDataCache[t];
                }
                else
                {
                    ReflectionCache.Type td = new ReflectionCache.Type(t, false);
                    TypeDataCache.Add(t, td);
                    return td;
                }
            }
        }
    }
}
