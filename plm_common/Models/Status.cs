using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models
{
    [Flags]
    public enum Status: long
    {
        NewColor = 1 << 0,
        NewStyle = 1 << 1,
        Updated = 1 << 2,

        All = ~0
    }
}
