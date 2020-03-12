using Neo4jIntegration.Attributes;
using Neo4jIntegration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo4jIntegration.Reflection
{
    public partial class ReflectionCache
    {
        public readonly struct Type
        {
            public readonly System.Type cachedType;
            public readonly INeo4jAttribute[] neo4JAttributes;
            public readonly Property? ID;
            public readonly Dictionary<string, Property> props;
            public readonly Dictionary<PropertyInfo, Property> propsInfoFirst;
            public IEnumerable<string> PropNames => props.Select(x => x.Key);
            public string Name => cachedType.Name;
           
            public Type(System.Type buildFrom)
            {
                cachedType = buildFrom;
                neo4JAttributes = buildFrom.GetCustomAttributes(true)
                    .Where(x => x is INeo4jAttribute)
                    .Cast<INeo4jAttribute>()
                    .ToArray();
                props = buildFrom.GetProperties()
                    .ToDictionary(
                        x => x.Name,
                        x => new Property(x)
                    );
                propsInfoFirst = props.ToDictionary(x => x.Value.info, x => x.Value);
                try
                {
                    ID = props.Where(x => x.Value.isID).Single().Value;
                }
                catch
                {
                    if (typeof(INeo4jNode).IsAssignableFrom(buildFrom))
                    {
                        throw new ArgumentException("All types of INeo4jNode must declare an ID (and only one ID)");
                    }
                    else
                    {
                        ID = null;
                    }
                }
            }
        }
    }
}
