using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class DbRequireParentAttribute: Attribute, INeo4jAttribute
    {
        public readonly bool parentNodeRequired;

        public DbRequireParentAttribute(bool parentNodeRequired = true)
        {
            this.parentNodeRequired = parentNodeRequired;
        }
    } 
}
