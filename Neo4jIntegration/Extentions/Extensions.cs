using Neo4jIntegration.Models;
using Neo4jIntegration.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo4jIntegration
{
    public static class Extensions
    {
        private static readonly Type enumerable = typeof(IEnumerable);
        private static readonly Type strng = typeof(string);
        public static bool IsEnumerable(this Type t)
        {
            return enumerable.IsAssignableFrom(t) && t != strng;
        }

        private static Dictionary<Type, string> labelsCache = new Dictionary<Type, string>();
        public static string QuerySaveLabels(this Type t)
        {
            string ret = "";
            if (labelsCache.ContainsKey(t))
            {
                ret = labelsCache[t];
            }
            else
            {
                ret = t.QueryLabels()
                        .Distinct()
                        .Aggregate((a, b) => $"{a}:{b}");

                lock (labelsCache)
                {
                    if (!labelsCache.ContainsKey(t))
                    {
                        labelsCache.Add(t, ret);
                    }
                }
            }
            return ret;
        }

        private static List<string> QueryLabels(this Type t, List<string> ret = null, List<Type> ts = null)
        {
            if (ret == null)
            {
                ret = new List<string>();
            }
            if (ts == null)
            {
                ts = new List<Type>();
            }
            //catch infinite recursion (when T: Iface<T>)
            if (ts.Contains(t))
            {
                return ret;
            }
            else
            {
                ts.Add(t);
            }

            if (t.IsGenericType)
            {
                ret.Add(t.Name.Split('`')[0]);
                foreach (Type g in t.GetGenericArguments())
                {
                    ret = g.QueryLabels(ret, ts);
                }
            }
            else
            {
                ret.Add(t.Name);
            }

            Type bse = t.BaseType;
            if (bse != null && bse != typeof(object))
            {
                ret = bse.QueryLabels(ret, ts);
            }

            foreach(Type i in t.GetInterfaces())
            {
                ret = i.QueryLabels(ret, ts);
            }

            return ret;
        }
        public static string QuerySaveName(this Type t)
        {
            return t.QuerySaveLabels().Replace(":", "__");
        }
        
        public static bool TryMakeGenericType(this Type a, out Type gen, params Type[] args)
        {
            try
            {
                gen = a.MakeGenericType(args);
                return true;
            }
            catch (System.ArgumentException ex) { }

            gen = null;
            return false;
        }
    }
}
