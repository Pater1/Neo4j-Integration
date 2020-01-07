using Neo4jClient.Cypher;
using plm_common.DB;
using plm_common.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace plm_common.Attributes
{
    public interface ICustomDBSchema
    {
        SaveQuearyParams<T> SaveValue<T>(SaveQuearyParams<T> qParams) where T : class, INeo4jNode, new();
        ReadQueryParams<T> ReadValue<T>(ReadQueryParams<T> rParams); //where T : class, INeo4jNode, new();
    }
    public interface ICustomDBSchema<T> where T : class, INeo4jNode, new()
    {
        SaveQuearyParams<T> SaveValue (SaveQuearyParams<T> qParams);
        ReadQueryParams<A> ReadValue<A>(ReadQueryParams<A> rParams);
    }
    public static class ICustomDBSchemaExtentions
    {
    }
}
