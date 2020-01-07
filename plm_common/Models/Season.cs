using plm_common.Models.Versioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models
{
    public class Season : INeo4jNode
    {
        public string Id { get; }
        public Versionable<SeasonCode> SeasonCode { get; private set; }
        public Versionable<DateTime> year { get; private set; }

        public bool IsActive { get; private set; } = true;
    }
}
