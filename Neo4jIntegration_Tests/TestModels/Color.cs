using Neo4jIntegration.Attributes;
using Neo4jIntegration.Models;
using Neo4jIntegration.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration_Tests.TestModels
{
    public class Color : INeo4jNode
    {
        [ID(IDAttribute.CollisionResolutionStrategy.Rand_Base64_10)]
        public string Id { get; set; }
        public bool IsActive { get; set; } = true;

        [DbName("HEX")]
        public Versionable<string> Hex { get; private set; } = new Versionable<string>();
        [DbName("NAME")]
        public Versionable<string> Name { get; private set; } = new Versionable<string>();

        [DbName("TEMPLATE")]
        public Versionable<Color> Template { get; private set; } = new Versionable<Color>();
    }
}
