using Neo4jClient;
using Neo4jClient.Transactions;
using System;
using System.Collections.Generic;
using System.Text;

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
