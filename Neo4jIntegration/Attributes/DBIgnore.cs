using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Neo4jIntegration.DB;

namespace Neo4jIntegration.Attributes
{
    public class DBIgnoreAttribute : Attribute, INeo4jAttribute, ICustomDBSchema
    {
        ReadQueryParams<T> ICustomDBSchema.ReadValue<T>(ReadQueryParams<T> rParams) { return rParams; }

        SaveQuearyParams<T> ICustomDBSchema.SaveValue<T>(SaveQuearyParams<T> qParams){ return qParams; }
    }
}
