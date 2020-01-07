using Neo4jIntegration.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    public class Fit : INeo4jNode
    {
        public string Id { get; private set; }
        public Versionable<string> Full { get; private set; }
        public Versionable<string> Klassik { get; private set; }
        public Versionable<string> Relaxed { get; private set; }
        public Versionable<string> Skinny { get; private set; }
        public Versionable<string> Straight { get; private set; }
        public Versionable<string> Tapered { get; private set; }
        public bool IsActive { get; private set; } = true;
    }
}
