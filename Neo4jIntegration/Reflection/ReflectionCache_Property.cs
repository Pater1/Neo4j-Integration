using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Neo4jIntegration.Reflection
{
    public partial class ReflectionCache
    {
        public readonly struct Property
        {
            public static readonly object[] fastEmpty = new object[0];
            public readonly bool isID;
            public readonly PropertyInfo info;
            public readonly INeo4jAttribute[] neo4JAttributes;
            public readonly string infoName;
            public readonly bool isCollection;
            public readonly string jsonName;

            public Property(PropertyInfo buildFrom)
            {
                info = buildFrom;

                neo4JAttributes = buildFrom.GetCustomAttributes(true)
                    .Where(x => x is INeo4jAttribute)
                    .Cast<INeo4jAttribute>()
                    .ToArray();

                isID = neo4JAttributes.Where(a => a is IDAttribute).Any();
                
                isCollection = typeof(IEnumerable).IsAssignableFrom(info.PropertyType) && info.PropertyType != typeof(string);

                infoName = info.Name;

                try
                {
                    jsonName = neo4JAttributes
                        .Select(x => x as DbNameAttribute)
                        .Where(x => x != null)
                        .SingleOrDefault()
                        ?.Name
                        ??
                        infoName;
                }
                catch
                {
                    throw new FormatException($"Property {info.DeclaringType.Name}.{info.Name} declares multiple DbNameAttributes! Properties may only declare one DbNameAttribute (note: IdAttribute also counts as a DbNameAttribute)");
                }
            }

            public object PullValue<T>(T instance)
            {
                if (instance.Equals(default(T)))
                {
                    return null;
                }

                var v = info.GetValue(instance, fastEmpty);
                return v;
            }
            public void PushValue<T>(T backingInstance, object value)
            {
                info.SetValue(backingInstance, value);
            }
        }
    }
}
