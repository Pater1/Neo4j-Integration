using Neo4j.Driver;
using Neo4jIntegration.Models;
using Neo4jIntegration.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neo4jIntegration.DB
{
    public static class LiveDbObjectExtentions
    {
        public static async Task<bool> Save<T>(this LiveDbObject<T> buildFor, Func<IDriver> graphClientFactory) where T : INeo4jNode
        {
            await DBOps.SaveNode(buildFor, graphClientFactory);
            return true;
        }
        public static async Task<LiveDbObject<T>> Save<T>(this T buildFor, Func<IDriver> graphClientFactory) where T : INeo4jNode
        {
            LiveDbObject<T> ret = LiveDbObject<T>.Build(buildFor, graphClientFactory, LiveObjectMode.Ignore);
            await ret.Save(graphClientFactory);
            return ret;
        }

        public static Task<LiveDbObject<T>> Save<T>(this T buildFor, IDriver graphClient) where T : INeo4jNode
        {
            return buildFor.Save(() => graphClient);
        }
        public static Task<bool> Save<T>(this LiveDbObject<T> buildFor, IDriver graphClient) where T : INeo4jNode
        {
            return buildFor.Save(() => graphClient);
        }
    }

}
