using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    [Flags]
    public enum Region : long 
    {
        US = 1 << 0, 
        UnitedStates = US,
        America = US,

        CA = 1 << 1,
        Canada = CA,

        UK = 1 << 2,
        UnitedKingdom = UK,

        AU = 1 << 3,
        Australia = AU,

        International = 1 << 4,


        All = ~0
    }
}
