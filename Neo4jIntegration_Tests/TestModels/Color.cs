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
        [ID(ID.IDType.String, ID.CollisionResolutionStrategy.Rand_Base62_10)]
        public string Id { get; set; }
        public bool IsActive { get; set; } = true;

        [ReferenceThroughRelationship("HEX")]
        public Versionable<string> Hex { get; private set; } = new Versionable<string>();
        [ReferenceThroughRelationship("NAME")]
        public Versionable<string> Name { get; private set; } = new Versionable<string>();

        [ReferenceThroughRelationship("TEMPLATE")]
        public Versionable<Color> Template { get; private set; } = new Versionable<Color>();
    }
}
