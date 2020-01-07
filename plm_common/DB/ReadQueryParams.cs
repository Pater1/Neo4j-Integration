using plm_common.Models;
using plm_common.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace plm_common.DB
{
    public struct ReadQueryParams<T>: IEquatable<ReadQueryParams<T>>, IEqualityComparer<ReadQueryParams<T>> //where T: class, INeo4jNode, new()
    {
        public ITypeWrappedCypherFluentQuery<T> typeWrappedCypherFluentQuery;
        public string parentName;
        public ReflectionCache.Property prop;
        public string childName;

        private Type type;

        public bool IsCollection => prop == null? false: prop.IsCollection;

        internal Type Type
        {
            get {
                return type == null ? prop.info.PropertyType : type;
            }
            set {
                type = value;
            }
        }

        public void AddIncludedChildNode()
        {
            typeWrappedCypherFluentQuery = typeWrappedCypherFluentQuery.AddIncludedChildNode(this);
        }

        public bool Equals([AllowNull] ReadQueryParams<T> other)
        {
            return other.childName == childName;
        }

        public bool Equals([AllowNull] ReadQueryParams<T> x, [AllowNull] ReadQueryParams<T> y)
        {
            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] ReadQueryParams<T> obj)
        {
            return obj.childName.GetHashCode();
        }

        public override int GetHashCode()
        {
            return this.GetHashCode(this);
        }
    }
}
