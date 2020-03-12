using Newtonsoft.Json;
using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Neo4jIntegration.Models.Versioning
{

    [System.Serializable]
    public class VersionableItteration<T> : INeo4jNode
    {
        private VersionableItteration<T> previous;
        private VersionableItteration<T> next;

        [ID(IDAttribute.CollisionResolutionStrategy.Rand_Base64_10)]
        public string Id { get; set; }
        public bool IsActive { get; set; } = true;

        [DbName("PREVIOUS")]
        public VersionableItteration<T> Previous
        {
            get => previous; set
            {
                previous = value;
                if (value.Next != this)
                {
                    value.Next = this;
                }
            }
        }

        [DbName("NEXT")]
        public VersionableItteration<T> Next { get => next; set
            {
                next = value;
                if (value.Previous != this)
                {
                    value.Previous = this;
                }
            }
        }

        public DateTime ItteratedTime { get; set; }

        [DbName("VALUE")]
        public T Value { get; set; }

        public AcceptanceState AcceptanceState { get; set; }

        public VersionableItteration(T value, AcceptanceState acceptanceState)
        {
            Value = value;
            AcceptanceState = acceptanceState;
            ItteratedTime = DateTime.UtcNow;
        }
        public VersionableItteration() { }

        public static implicit operator T(VersionableItteration<T> vit)
        {
            return vit.Value;
        }
    }
}
