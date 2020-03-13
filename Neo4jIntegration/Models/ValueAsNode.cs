using Neo4jIntegration.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    [DbRequireParent]
    public class ValueAsNode<T> : INeo4jNode where T: unmanaged
    {
        [ID(IDAttribute.CollisionResolutionStrategy.Long_DateTime)]
        public string Id { get; set; }
        public bool IsActive { get; set; }
        public T Value { get; set; }
    }
}
