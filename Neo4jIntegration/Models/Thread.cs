using Neo4jIntegration.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    public class Thread : INeo4jNode
    {
        public string Id { get; private set; }
        public Versionable<string> name { get; private set; }
        public bool IsActive { get; private set; } = true;
    }
}
