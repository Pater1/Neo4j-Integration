using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    [Flags]
    public enum ProductType: long
    {
        Knit = 1 << 0,
        Woven = 1 << 1,
    }
}
