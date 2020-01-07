using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models.Versioning
{
    public enum AcceptanceState: byte
    {
        RolledBack,
        Suggested,
        Accepted,
        Finalized,
        Rejected
    }
}
