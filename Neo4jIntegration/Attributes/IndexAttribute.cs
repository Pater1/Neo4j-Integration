using System;

namespace Neo4jIntegration.Attributes
{
    public class IndexAttribute : Attribute, INeo4jAttribute
    {
        string indexName;
        public IndexAttribute(string indexName)
        {
            this.indexName = indexName;
        }
    }
}
