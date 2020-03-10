using Neo4jIntegration.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    public interface INeo4jNode
    {
        [ID(ID.IDType.String, ID.CollisionResolutionStrategy.ErrorOut)]
        public string Id { get; }
        bool IsActive { get; }
    }
}
