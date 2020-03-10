using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models.Metadata;
using Neo4jIntegration.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace Neo4jIntegration.Models
{
    public class ReferenceCollection<T> : NoDBLabel, INeo4jNode, ICollection<T>, ICollection<ReflectReadDictionary<T>>, ICustomDBSchema<ReferenceCollection<T>> //where T: class, INeo4jNode, new()
    {
        [ID(ID.IDType.String, ID.CollisionResolutionStrategy.Rand_Base62_10)]
        public string Id { get; private set; }
        public bool IsActive { get; private set; } = true;

        private List<ReflectReadDictionary<T>> backingCollection;
        public ReferenceCollection(IEnumerable<T> backingCollection)
        {
            this.backingCollection = backingCollection.Select(x => new ReflectReadDictionary<T>(x)).ToList();
        }
        public ReferenceCollection(IEnumerable<ReflectReadDictionary<T>> backingCollection)
        {
            this.backingCollection = backingCollection.ToList();
        }
        public ReferenceCollection()
        {
            this.backingCollection = new List<ReflectReadDictionary<T>>();
        }
        public ReferenceCollection(Func<T, T, int> sort)
        {
            this.backingCollection = new List<ReflectReadDictionary<T>>();
            this.sort = new FuncSort(sort);
        }
        private readonly FuncSort sort;
        private readonly struct FuncSort : IComparer<ReflectReadDictionary<T>>
        {
            public readonly bool validSort;
            private readonly Func<ReflectReadDictionary<T>, ReflectReadDictionary<T>, int> sort;
            public FuncSort(Func<T, T, int> sort)
            {
                this.sort = (a, b) => sort(a.backingInstance, b.backingInstance);
                validSort = true;
            }
            public int Compare([AllowNull] ReflectReadDictionary<T> x, [AllowNull] ReflectReadDictionary<T> y)
            {
                return sort(x, y);
            }
        }

        [JsonIgnore]
        public int Count => backingCollection.Count;

        [JsonIgnore]
        public bool IsReadOnly => false;

        private struct NodeDeDup : IEqualityComparer<ReflectReadDictionary<T>>
        {
            public bool Equals([AllowNull] ReflectReadDictionary<T> x, [AllowNull] ReflectReadDictionary<T> y)
            {
                T xbi = x.backingInstance;
                T ybi = y.backingInstance;

                if (xbi is INeo4jNode && ybi is INeo4jNode)
                {
                    return ((INeo4jNode)xbi).Id == ((INeo4jNode)ybi).Id;
                }
                else if (!(xbi is INeo4jNode) && !(ybi is INeo4jNode))
                {
                    return xbi.Equals(ybi);
                }
                else
                {
                    return false;
                }
            }

            public int GetHashCode([DisallowNull] ReflectReadDictionary<T> obj)
            {
                T xbi = obj.backingInstance;

                if (xbi is INeo4jNode)
                {
                    return (int)((INeo4jNode)xbi).Id.GetHashCode();
                }
                else
                {
                    return xbi.GetHashCode();
                }
            }
        }
        public void DeDup()
        {
            backingCollection = backingCollection.Distinct(new NodeDeDup()).ToList();
        }
        public void Add(T item)
        {
            backingCollection.Add(new ReflectReadDictionary<T>(item));
            if (sort.validSort)
            {
                backingCollection.Sort(sort);
            }
        }

        public void Clear()
        {
            backingCollection.Clear();
        }

        public bool Contains(T item)
        {
            return backingCollection.Contains(new ReflectReadDictionary<T>(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            backingCollection.Select(x => x.backingInstance).ToList().CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return backingCollection.Select(x => x.backingInstance).GetEnumerator();
        }

        public bool Remove(T item)
        {
            return backingCollection.Remove(new ReflectReadDictionary<T>(item));
        }

        private const string relationship = "ITEM";

        private class WriteWrap : INeo4jNode
        {
            public WriteWrap(INeo4jNode wrapped)
            {
                Wrapped = new ReflectReadDictionary<INeo4jNode>(wrapped);
            }
            public WriteWrap() { }

            [ID(ID.IDType.String, ID.CollisionResolutionStrategy.ErrorOut)]
            public string Id
            {
                get
                {
                    return Wrapped.Get(x => x.Id);
                }
                set
                {
                    Wrapped.backingObject.GetType().GetProperty("Id").SetValue(Wrapped.backingObject, value);
                }
            }
            public bool IsActive
            {
                get
                {
                    return Wrapped.Get(x => x.IsActive);
                }
                set
                {
                    Wrapped.backingObject.GetType().GetProperty("IsActive").SetValue(Wrapped.backingObject, value);
                }
            }
            public string __type__ => $"\"{Wrapped.backingObject.GetType().QuerySaveName()}\"";

            private ReflectReadDictionary<INeo4jNode> Wrapped { get; set; }
        }
        public SaveQuearyParams<ReferenceCollection<T>> SaveValue(SaveQuearyParams<ReferenceCollection<T>> qParams)
        {
            if (string.IsNullOrWhiteSpace(this.Id))
            {
                this.Id = Guid.NewGuid().ToString("n");
            }

            WriteWrap ww = new WriteWrap(this);

            var workingParams = qParams;

            //workingParams = (new ReferenceThroughRelationship("COLLECTION") { explicitNode = ww }).SaveValue(workingParams.CrosstypedCopy(new ReflectReadDictionary<ReferenceCollection<T>>(this), 1)); //workingParams.ChainSaveNode(this);

            workingParams = workingParams.ChainSaveNode(ww);
            workingParams = DBOps<ReferenceCollection<T>>.Writes<ReferenceCollection<T>>
                .WriteRelationship(workingParams, qParams.objName, workingParams.objName, "COLLECTION");

            string nodeName = workingParams.objName;

            foreach (ReflectReadDictionary<T> single in backingCollection)
            {
                if (single.backingInstance is INeo4jNode)
                {
                    var singleParams = workingParams.ChainSaveNode((INeo4jNode)single.backingInstance);
                    workingParams = DBOps<ReferenceCollection<T>>.Writes<ReferenceCollection<T>>.WriteRelationship(singleParams, nodeName, singleParams.objName, relationship);
                }
            }

            workingParams.objName = qParams.objName;

            return workingParams;
        }
        public ReadQueryParams<A> ReadValue<A>(ReadQueryParams<A> rParams)
        {
            return DBOps<T>.Reads.ReadRelationship(rParams, relationship);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return backingCollection.GetEnumerator();
        }

        public void Add(ReflectReadDictionary<T> item)
        {
            backingCollection.Add(item);
        }

        public bool Contains(ReflectReadDictionary<T> item)
        {
            return backingCollection.Contains(item);
        }

        public void CopyTo(ReflectReadDictionary<T>[] array, int arrayIndex)
        {
            backingCollection.CopyTo(array, arrayIndex);
        }

        public bool Remove(ReflectReadDictionary<T> item)
        {
            return backingCollection.Remove(item);
        }

        IEnumerator<ReflectReadDictionary<T>> IEnumerable<ReflectReadDictionary<T>>.GetEnumerator()
        {
            return backingCollection.GetEnumerator();
        }

    }
}
