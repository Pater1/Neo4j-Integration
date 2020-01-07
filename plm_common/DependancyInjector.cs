using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace plm_common
{
    public struct DependencyInjector
    {
        private readonly static DependencyInjector instance = new DependencyInjector();

        private static Dictionary<Type, object> dependencies = new Dictionary<Type, object>();
        public static T Get<T>()
        {
            return (T)dependencies[typeof(T)];
        }
        public static void Insert(object dependency)
        {
            dependencies.Add(dependency.GetType(), dependency);
        }

        public static DependencyInjector Custom()
        {
            return new DependencyInjector(instance);
        }
        private DependencyInjector(DependencyInjector clone) {
            if (clone.namedDependencies != null)
            {
                namedDependencies = clone.namedDependencies.ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                namedDependencies = new Dictionary<string, object>();
            }
        }

        private Dictionary<string, object> namedDependencies;
        public T Get<T>(string key)
        {
            return (T)Get(key);
        }
        public object Get(string key)
        {
            return namedDependencies[key];
        }
        public DependencyInjector Insert<T>(string key, T dependency)
        {
            if (namedDependencies.ContainsKey(key))
            {
                namedDependencies[key] = dependency;
            }
            else
            {
                namedDependencies.Add(key, dependency);
            }
            return this;
        }

        public DependencyInjector Clone()
        {
            return new DependencyInjector(this);
        }
        public void Set<T>(string key, T dependency)
        {
            namedDependencies[key] = dependency;
        }
    }

    public static class DependencyInjectorExtentions
    {
    }
}
