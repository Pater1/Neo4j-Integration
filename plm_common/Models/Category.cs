using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models
{
    [Flags]
    public enum Category: long
    {
        Bags = 1 << 0,
        Dress = 1 << 1,
        Headwear = 1 << 2,
        Jacket = 1 << 3,
        Legging = 1 << 4,
        LongSleeve = 1 << 5,
        Miscellaneous = 1 << 6,
        Pants = 1 << 7,
        ShortSleeve = 1 << 8,
        Shorts = 1 << 9,
        Skirts = 1 << 10,
        Sleeveless = 1 << 11,
        Underwear = 1 << 12,
        Vest = 1 << 13,

        All = ~0
    }
}
