using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    [Flags]
    public enum Market: long
    {
        Wholesale = 1 << 0,
        Web = 1 << 1,
        REI = 1 << 2,
        Scheels  = 1 << 3,
        T9 = 1 << 4,

        All = ~0
    }
}
