using plm_common.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models
{
    public class StyleColorRelationship : INeo4jNode
    {
        public string Id { get; private set; }
        public Versionable<string> CustomHex { get; private set; }
        public Versionable<float> HasSaleSamples { get; private set; }
        public Versionable<float> IsNew { get; private set; }
        public Versionable<float> IsKore { get; private set; }
        public Versionable<DateTime> SketchLastExported { get; private set; }
        public Versionable<string> StyleGrade { get; private set; }
        public Versionable<float> EarlyDelivery { get; private set; }

        public bool IsActive { get; private set; } = true;
    }
}
