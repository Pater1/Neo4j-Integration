using Neo4jClient.Cypher;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Neo4jIntegration.Attributes
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
