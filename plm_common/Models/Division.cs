using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models
{
    [Flags]
    public enum Division: long
    {
        Boys = 1 << 0,
        Girls = 1 << 1,
        Kids = Boys|Girls,

        Mens = 1 << 2,
        Womens = 1 << 3,
        Adults = Mens|Womens,

        Unisex = 1 << 4,

         All = ~0,
    }
}
