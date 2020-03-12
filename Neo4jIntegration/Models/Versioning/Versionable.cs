using Newtonsoft.Json;
using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo4jIntegration.Models.Versioning
{
    public class Versionable<T> : INeo4jNode, IEnumerable<VersionableItteration<T>>, IEnumerable<T>
    {
        [ID(ID.IDType.String, ID.CollisionResolutionStrategy.Rand_Base62_10)]
        public string Id { get; set; }
        public bool IsActive { get; set; } = true;

        public VersionableItteration<T> Start
        {
            get => start; private set
            {
                start = value;
                if (Last == null)
                {
                    Last = value;
                }
            }
        }
        public VersionableItteration<T> Last
        {
            get => last; private set
            {
                last = value;
                if (Start == null)
                {
                    Start = value;
                }
            }
        }

        public void Add(T value, AcceptanceState acceptanceState)
        {
            Add(new VersionableItteration<T>(value, acceptanceState));
        }
        public void Add(VersionableItteration<T> value)
        {
            if (Start == null || Last == null)
            {
                Start = value;
                Last = value;
            }
            else
            {
                var latest = Last;
                value.Previous = latest;
                latest.Next = value;
                Last = value;
            }
        }

        public IEnumerator<VersionableItteration<T>> GetEnumerator()
        {
            VersionableItteration<T> current = Start;
            while (current != null)
            {
                yield return current;
                current = current.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.Cast<T>().GetEnumerator();
        }

        private VersionableItteration<T> last;
        private VersionableItteration<T> start;

        public static implicit operator VersionableItteration<T>(Versionable<T> vit)
        {
            return vit.Last;
        }
    }
}
