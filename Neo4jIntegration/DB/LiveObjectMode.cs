using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.DB
{
    [Flags]
    public enum LiveObjectMode: byte
    {
        //Write data back to the database as soon as its value is set
        LiveWrite = 1 << 0,
        //Write value to local object immediately, write to database in the background
        DeferedWrite = 1 << 2,
        //Write the data back to the database on explicit Save() call only
        IgnoreWrite = 1 << 3,

        //Read values directly from the database
        LiveRead = 1 << 4,
        //Read values from the local object, then fire a cache-update query in the background (AKA: eventually consistent)
        DeferedRead = 1 << 5,
        //Read values from the local object only (will still hit the database when pulling a node not yet in the local cache)
        IgnoreRead = 1 << 6,

        Live = LiveWrite | LiveRead,
        Defered = DeferedWrite | DeferedRead,
        Ignore = IgnoreWrite | IgnoreRead
    }
}
