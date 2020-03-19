using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Neo4jIntegration;
using Neo4jIntegration.DB;
using Neo4jIntegration.Models;
using Neo4jIntegration.Models.Versioning;
using Neo4jIntegration.Reflection;
using Neo4jIntegration_Tests.TestModels;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace plm_testing
{
    [TestClass]
    public class Neo4jIntegrationTests
    {
        [TestMethod]
        public async Task TestInsertStyle()
        {
            Style s = new Style();
            s.Id = (new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)).ToString("n");
            s.Name = "Concrete";
            s.Category.Add(Category.Bags, AcceptanceState.Suggested);
            s.Category.Add(Category.Headwear, AcceptanceState.Suggested);
            s.Category.Add(Category.Legging, AcceptanceState.Suggested);
            s.WaistLine.Add(new List<float>(new float[] { 0, 1, 2.2f }), AcceptanceState.Suggested);
            Color c = new Color();
            c.Id = (new Guid(100, 99, 98, 97, 96, 95, 94, 93, 92, 91, 90)).ToString("n");
            c.Hex.Add("ABC123", AcceptanceState.Suggested);
            Color c2 = new Color();
            c2.Id = (new Guid(50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40)).ToString("n");
            c2.Hex.Add("DEF987", AcceptanceState.Accepted);
            c.Template.Add(c2, AcceptanceState.RolledBack);
            s.Colors.Add(new List<Color>(new Color[] { c }), AcceptanceState.Suggested);
            s.Template = new Style()
            {
                Name = "Template",
                Id = (new Guid(10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20)).ToString("n"),
                Template = new Style()
                {
                    Id = (new Guid(20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30)).ToString("n"),
                    Name = "Super Template"
                }
            };


            Func<IDriver> client = () =>
            {
                IDriver drvr = GraphDatabase.Driver(
                    "bolt://localhost:7687"
                    , AuthTokens.Basic("neo4j", "password")
                );
                return drvr;
            };

            //await s.Save(client);

            var a = Neo4jSet<Style>.All(client)
                .Include(x => x.Template.Template)

                .Include(x => x.Category.Start.Value)
                .Include(x => x.Category.Start.Next.Value)
                .Include(x => x.Category.Start.Next.Next.Value)

                .Include(x => x.Colors)
                .ThenInclude(x => x.Start)
                .ThenInclude(x => x.Value)

                .Include(x => x.Colors)
                .ThenInclude(x => x.Start)
                .ThenInclude(x => x.Next)
                .ThenInclude(x => x.Value)
                .ThenIncludeCollection(x => x, x => x.Hex)
                .ThenInclude(x => x.Start)
                .ThenInclude(x => x.Next)
                .ThenInclude(x => x.Value)

                //.Where(x => x.Id == (new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)).ToString("n"));
                ;

            LiveDbObject<Style>[] sOut = (await a.ReturnAsync()).ToArray();
            Style[] sCache = sOut.Select(x => x.BackingInstance).ToArray();

            LiveDbObject<Style> templt = sOut.Single(x =>
            {
                x.LiveMode = LiveObjectMode.Live;
                return x.Get(y => y.Name).Result == "Template";
            });

            await templt.Call(x => x.Colors, (y, a) => y.Add(a, AcceptanceState.Suggested), new List<Color>() { c2 } as ICollection<Color>);

            LiveDbObject<Style> suprTemplt = sOut.Single(x =>
            {
                x.LiveMode = LiveObjectMode.Live;
                return x.Get(y => y.Name).Result == "Super Template";
            });

            Color c3 = new Color();
            c3.Hex.Add("dahex", AcceptanceState.RolledBack);
            c3.Template.Add(c2, AcceptanceState.Finalized);
            await suprTemplt.Call(x => x.Colors, (y, a) => y.Add(a, AcceptanceState.Suggested), new List<Color>() { c, c2, c3 } as ICollection<Color>);
        }
    }
}
