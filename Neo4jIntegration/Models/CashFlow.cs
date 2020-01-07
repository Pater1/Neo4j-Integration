using Neo4jIntegration.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    public class CashFlow : INeo4jNode
    {
        public string Id { get; private set; }
        public bool IsActive { get; private set; } = true;
        public Versionable<float> MSRP { get; private set; }
        public Versionable<float> CostToManufacture { get; private set; }
        public Versionable<float> Landed { get; private set; }
        public Versionable<string> RegionCode { get; private set; }
        public Versionable<float> Wholesale { get; private set; }
        public Versionable<float> Fob { get; private set; }
    }
}
