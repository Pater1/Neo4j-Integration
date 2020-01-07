using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Models
{
    public interface ITemplatable<T>: INeo4jNode
    {
        public T Template { get; set; }
    }
    public class TemplateRelationship<T> : Relationship, IRelationshipAllowingSourceNode<T>, IRelationshipAllowingTargetNode<T>
    {
        public const string TypeKey = "Template";

        public TemplateRelationship(NodeReference targetNode)
        : base(targetNode)
        { }
        public TemplateRelationship(): base(null)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }
}
