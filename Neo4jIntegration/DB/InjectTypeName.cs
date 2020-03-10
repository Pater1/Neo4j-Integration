using Neo4jIntegration.Attributes;
using Neo4jIntegration.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static Neo4jIntegration.Reflection.ReflectionCache;

namespace Neo4jIntegration.DB
{
    public readonly ref struct InjectTypeName
    {
        private readonly string TypeName;
        public InjectTypeName(System.Type type)
        {
            TypeName = type.QuerySaveName();
        }
        public SaveQuearyParams<T> SaveValue<T>(SaveQuearyParams<T> qParams)where T: class, INeo4jNode, new()
        {
            qParams.prop = Property.Dummy(WrittenTo: false);
            var ret = DBOps<T>.Writes<T>.WriteSingleInline(qParams, "__type__", $"\"{TypeName}\"");
            return ret;
        }
    }
}
