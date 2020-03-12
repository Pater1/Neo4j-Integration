using Neo4jClient.Transactions;
using Neo4jIntegration.Models;
using Neo4jIntegration.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.DB
{
    public static class LiveDbObjectExtentions
    {
        public static bool Save<T>(this LiveDbObject<T> buildFor, Func<ITransactionalGraphClient> graphClientFactory) where T : INeo4jNode
        {
            DBOps.SaveNode(buildFor, graphClientFactory);
            return true;
        }
        public static LiveDbObject<T> Save<T>(this T buildFor, Func<ITransactionalGraphClient> graphClientFactory) where T : INeo4jNode
        {
            LiveDbObject<T> ret = buildFor.Build(graphClientFactory);
            ret.Save(graphClientFactory);
            return ret;
        }

        public static LiveDbObject<T> Save<T>(this T buildFor, ITransactionalGraphClient graphClient) where T : INeo4jNode
        {
            return buildFor.Save(() => graphClient);
        }
        public static bool Save<T>(this LiveDbObject<T> buildFor, ITransactionalGraphClient graphClient) where T : INeo4jNode
        {
            return buildFor.Save(() => graphClient);
        }

        public static LiveDbObject<T> Build<T>(this T buildFor, Func<ITransactionalGraphClient> graphClient) where T : INeo4jNode
        {
            return new LiveDbObject<T>(buildFor, graphClient);
        }
    }

}
