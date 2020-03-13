using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models;

namespace Neo4jIntegration.Attributes
{
    public class DbNameAttribute : Attribute, INeo4jAttribute
    {
        private static Dictionary<Type, string> nameCache = new Dictionary<Type, string>();
        public string Name { get; private set; }
        public DbNameAttribute(Type relationshipType)
        {
            if (nameCache.ContainsKey(relationshipType))
            {
                Name = nameCache[relationshipType];
            }
            else
            {
                var relType = relationshipType.GetField("TypeKey", (BindingFlags)(~0)).GetValue(null);
                if (relType != null)
                {
                    Name = relType.ToString();
                }
                else
                {
                    Name = relationshipType.QuerySaveName();
                }
                nameCache.Add(relationshipType, Name);
            }
        }
        public DbNameAttribute(string relationshipLabel)
        {
            Name = relationshipLabel;
        }

        public INeo4jNode explicitNode { get; set; } = null;

    }
}
