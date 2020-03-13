using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    public interface ITemplatable<T>: INeo4jNode
    {
        public T Template { get; set; }
    }
}
