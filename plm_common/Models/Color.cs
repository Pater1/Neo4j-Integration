using plm_common.Attributes;
using plm_common.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models
{
    public class Color : INeo4jNode
    {
        [ID(ID.IDType.String, ID.CollisionResolutionStrategy.Rand_Base62_10)]
        public string Id { get; private set; }
        public bool IsActive { get; private set; } = true;

        [ReferenceThroughRelationship("HEX")]
        public Versionable<string> Hex { get; private set; } = new Versionable<string>();
        [ReferenceThroughRelationship("NAME")]
        public Versionable<string> Name { get; private set; } = new Versionable<string>();

        [ReferenceThroughRelationship("TEMPLATE")]
        public Versionable<Color> Template { get; private set; } = new Versionable<Color>();
    }
}
