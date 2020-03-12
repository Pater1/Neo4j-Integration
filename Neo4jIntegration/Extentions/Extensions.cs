using Neo4jIntegration.Models;
using Neo4jIntegration.Models.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo4jIntegration
{
    public static class Extensions
    {
        private static readonly Type noEnum = typeof(NoDBCollection);
        private static readonly Type enumerable = typeof(IEnumerable);
        private static readonly Type strng = typeof(string);
        public static bool IsEnumerable(this Type t)
        {
            return enumerable.IsAssignableFrom(t) && t != strng && !noEnum.IsAssignableFrom(t);
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
                if (t.IsGenericType)
                {
                    ret = t.Name.Split('`')[0];
                    ret = t.GetGenericArguments().Select(x => QuerySaveLabels(x)).Prepend(ret).Aggregate((a, b) =>
                    {
                        string delimiter =
                            string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) ? "" : ":";
                        return $"{a}{delimiter}{b}";
                    });
                }
                else
                {
                    ret = t.Name;
                }

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
        public static string QuerySaveName(this Type t)
        {
            return t.QuerySaveLabels().Replace(":", "__");
        }
        //public static string QuerySaveLabel(this Type t)
        //{
        //    return t.QuerySaveLabels().Split(':')[0];
        //}

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
