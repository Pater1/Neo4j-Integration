using Neo4jIntegration.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    public class Season : INeo4jNode
    {
        public string Id { get; }
        public Versionable<SeasonCode> SeasonCode { get; private set; }
        public Versionable<DateTime> year { get; private set; }

        public bool IsActive { get; private set; } = true;
    }
}
