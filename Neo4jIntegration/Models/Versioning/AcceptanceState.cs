using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models.Versioning
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
