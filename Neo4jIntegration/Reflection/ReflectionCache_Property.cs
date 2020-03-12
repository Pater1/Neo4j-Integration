using Neo4jIntegration.Attributes;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Neo4jIntegration.Reflection
{
    public partial class ReflectionCache
    {
        public class Property
        {
            public static object[] fastEmpty = new object[0];
            public bool isID;
            public readonly PropertyInfo info;
            public readonly INeo4jAttribute[] neo4JAttributes;

            public static Property Dummy(bool WrittenTo = false)
            {
                Property ret = new Property(null);
                ret.WrittenTo = WrittenTo;
                return ret;
            }

            public bool WrittenTo { get; internal set; }

            public string Name => info.Name;
            
            public bool IsCollection => typeof(IEnumerable).IsAssignableFrom(info.PropertyType) && !typeof(NoDBCollection).IsAssignableFrom(info.PropertyType) && info.PropertyType != typeof(string);

            public string JsonName => neo4JAttributes
                .Select(x => x as DbNameAttribute)
                .Where(x => x != null)
                .FirstOrDefault()
                ?.Name
                ?.ToLowerInvariant() 
                ?? 
                Name.ToLowerInvariant();

            public Property(PropertyInfo buildFrom, bool noCheck)
            {
                info = buildFrom;
                neo4JAttributes = buildFrom.GetCustomAttributes(true)
                    .Where(x => x is INeo4jAttribute)
                    .Cast<INeo4jAttribute>()
                    .ToArray();
                WrittenTo = false;
                //try
                //{
                //    customDBSchema = neo4JAttributes.Where(a => typeof(ICustomDBSchema).IsAssignableFrom(a.GetType())).SingleOrDefault() as ICustomDBSchema;
                //    if (customDBSchema == null)
                //    {
                //        customDBSchema = new StoreDirectAttribute();
                //    }
                //}
                //catch
                //{
                //    if (!noCheck)
                //    {
                //        throw new ArgumentException("A column in the database may only declare one ICustomDBSchema");
                //    }
                //}
            }

            public object PullValue<T>(T instance)
            {
                var v = info.GetValue(instance, fastEmpty);
                return v;
            }
            public void PushValue<T>(T backingInstance, object value)
            {
                info.SetValue(backingInstance, value);
                WrittenTo = true;
            }

            private Property(Property buildFrom)
            {
                if (buildFrom == null) return;

                info = buildFrom.info;
                neo4JAttributes = buildFrom.neo4JAttributes; //clone
                WrittenTo = false;
            }
            public Property DeepClone()
            {
                return new Property(this);
            }
        }
    }
}
