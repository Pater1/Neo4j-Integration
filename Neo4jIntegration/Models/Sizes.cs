using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    [Flags]
    public enum Sizes: long
    {
        OS = 1 << 0,
        OneSize = OS,

        XXS = 1 << 1,
        
        XS =  1 << 2,

        S = 1 << 3,

        M = 1 << 4,
        
        L = 1 << 5,

        XL = 1 << 6,

        XXL = 1 << 7,

        XXXL = 1 << 8,
        
        XXXXL = 1 << 9,

        XXXXXL = 1 << 10,

        All = ~0,
    }
}
