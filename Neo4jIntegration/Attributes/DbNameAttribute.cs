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
        public DbNameAttribute(string relationshipLabel)
        {
            if(relationshipLabel == "ITEM")
            {
                throw new ArgumentException("The DbName \"ITEM\" is reserved for the serialization of collections! Please choose another DbName");
            }
            Name = relationshipLabel;
        }

        public INeo4jNode explicitNode { get; set; } = null;

    }
}
