using Neo4jIntegration.Attributes;

namespace Neo4jIntegration.Models
{
    public interface INeo4jNode
    {
        [ID(IDAttribute.CollisionResolutionStrategy.ErrorOut)]
        public string Id { get; set; }
        bool IsActive { get; set; }
    }
}
