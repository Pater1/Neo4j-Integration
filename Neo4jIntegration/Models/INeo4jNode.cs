using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    public interface INeo4jNode
    {
        public string Id { get; }
        bool IsActive { get; }
    }
}
