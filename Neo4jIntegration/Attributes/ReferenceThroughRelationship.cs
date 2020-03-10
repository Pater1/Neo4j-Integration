using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Neo4jClient;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models;

using RelDir = Neo4jIntegration.DB.RelationshipDirection;

namespace Neo4jIntegration.Attributes
{
    public class ReferenceThroughRelationship : Attribute, INeo4jAttribute, ICustomDBSchema
    {
        private static Dictionary<Type, string> nameCache = new Dictionary<Type, string>();
        public string Relationship { get; private set; }
        private RelDir relationshipDirection;
        public ReferenceThroughRelationship(Type relationshipType, RelDir relationshipDirection = RelDir.Forward)
        {
            if (nameCache.ContainsKey(relationshipType))
            {
                Relationship = nameCache[relationshipType];
            }
            else
            {
                var relType = relationshipType.GetField("TypeKey", (BindingFlags)(~0)).GetValue(null);
                if (relType != null)
                {
                    Relationship = relType.ToString();
                }
                else
                {
                    Relationship = relationshipType.QuerySaveName();
                }
                nameCache.Add(relationshipType, Relationship);
            }
            this.relationshipDirection = relationshipDirection;
        }
        public ReferenceThroughRelationship(string relationshipLabel, RelDir relationshipDirection = RelDir.Forward)
        {
            Relationship = relationshipLabel;
            this.relationshipDirection = relationshipDirection;
        }

        public INeo4jNode explicitNode { get; set; } = null;

        public SaveQuearyParams<T> SaveValue<T>(SaveQuearyParams<T> qParams) where T : class, INeo4jNode, new()
        {
            var RQParams = qParams.ChainSaveNode(explicitNode);

            if (RQParams.success)
            {
                RQParams = DBOps<T>.Writes<T>.WriteRelationship(RQParams, qParams.objName, RQParams.objName, Relationship, relationshipDirection);
                qParams.queary = RQParams.queary;
            }

            return qParams;
        }

        ReadQueryParams<T> ICustomDBSchema.ReadValue<T>(ReadQueryParams<T> rParams)
        {
            return DBOps<T>.Reads.ReadRelationship(rParams, Relationship, relationshipDirection);
        }
    }
}
