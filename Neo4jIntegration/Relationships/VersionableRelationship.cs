using Neo4jClient;

namespace Neo4jIntegration.Models
{
    public class VersionableRelationship<T> : Relationship, IRelationshipAllowingSourceNode<T>, IRelationshipAllowingTargetNode<T>
    {
        public const string TypeKey = "Versions";

        public VersionableRelationship(NodeReference targetNode) : base(targetNode)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }

    public class StartVersioningRelationship<T> : Relationship, IRelationshipAllowingSourceNode<T>, IRelationshipAllowingTargetNode<T>
    {
        public const string TypeKey = "StartingVersion";

        public StartVersioningRelationship(NodeReference targetNode) : base(targetNode)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }
    public class CurrentVersioningRelationship<T> : Relationship, IRelationshipAllowingSourceNode<T>, IRelationshipAllowingTargetNode<T>
    {
        public const string TypeKey = "CurrentVersion";

        public CurrentVersioningRelationship(NodeReference targetNode) : base(targetNode)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }

    public class NextVersioningRelationship<T> : Relationship, IRelationshipAllowingSourceNode<T>, IRelationshipAllowingTargetNode<T>
    {
        public const string TypeKey = "NextVersion";

        public NextVersioningRelationship(NodeReference targetNode) : base(targetNode)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }
    public class PreviousVersioningRelationship<T> : Relationship, IRelationshipAllowingSourceNode<T>, IRelationshipAllowingTargetNode<T>
    {
        public const string TypeKey = "PreviousVersion";

        public PreviousVersioningRelationship(NodeReference targetNode) : base(targetNode)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }
}