using plm_common.Attributes;
using plm_common.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models
{
    public class Fabric : INeo4jNode
    {
        public string Id { get; private set; }
        [ReferenceThroughRelationship("Threads")]
        public Versionable<ICollection<Thread>> Threads { get; private set; }
        public Versionable<float> GSM { get; private set; }
        public Versionable<string> Logo { get; private set; }
        public Versionable<float> IsInsulation { get; private set; }
        public Versionable<string> BrandName { get; private set; }
        public Versionable<Thread> Thread { get; private set; }


        [ReferenceThroughRelationship("FABRICWEIGHT")]
        public Versionable<float> Weight { get; private set; } = new Versionable<float>();

        public bool IsActive { get; private set; } = true;
    }
}
