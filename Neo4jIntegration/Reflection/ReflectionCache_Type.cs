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
        public class Type
        {
            public System.Type cachedType;
            public INeo4jAttribute[] neo4JAttributes;
            public Property ID;
            public Dictionary<string, Property> props;
            public Dictionary<PropertyInfo, Property> propsInfoFirst;
            public IEnumerable<Property> WritePropsList => props.Select(x => x.Value).Except(new ReflectionCache.Property[] { ID }).Prepend(ID);
            public IEnumerable<string> PropNames => props.Select(x => x.Key);
            public string Name => cachedType.Name;
            public IEnumerable<IOnReadAttribute> onReads => neo4JAttributes.Where(x => x is IOnReadAttribute).Cast<IOnReadAttribute>();
            public IEnumerable<IOnWriteAttribute> onWrites => neo4JAttributes.Where(x => x is IOnWriteAttribute).Cast<IOnWriteAttribute>();

            public Type(System.Type buildFrom, bool noCheck)
            {
                cachedType = buildFrom;
                neo4JAttributes = buildFrom.GetCustomAttributes(true)
                    .Where(x => x is INeo4jAttribute)
                    .Cast<INeo4jAttribute>()
                    .ToArray();
                var p = buildFrom.GetProperties();
                props = p
                    .ToDictionary(
                        x => x.Name.ToLower(),
                        x => new Property(x, noCheck)
                    );
                propsInfoFirst = props.ToDictionary(x => x.Value.info, x => x.Value);
                try
                {
                    ID = props.Where(x => x.Value.neo4JAttributes.Where(a => a is ID).Any()).Single().Value;
                    props.Remove(ID.Name.ToLower());
                    ID.isID = true;
                }
                catch
                {
                    if (!noCheck && typeof(INeo4jNode).IsAssignableFrom(buildFrom))
                    {
                        throw new ArgumentException("All types of INeo4jNode must declare an ID (and only one ID)");
                    }
                }
            }
            private Type(Type buildFrom)
            {
                cachedType = buildFrom.cachedType;
                neo4JAttributes = buildFrom.neo4JAttributes.ToArray(); //clone
                props = buildFrom.props.ToDictionary(x => x.Key, x => x.Value.DeepClone()); //clone
                propsInfoFirst = props.ToDictionary(x => x.Value.info, x => x.Value);
                ID = buildFrom.ID;
            }

            public Type DeepClone()
            {
                return new Type(this);
            }
        }
    }
}
