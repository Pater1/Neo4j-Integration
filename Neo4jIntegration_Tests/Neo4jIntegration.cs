using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver.V1;
using Neo4jClient;
using Neo4jClient.Cypher;
using Neo4jClient.Transactions;
using Neo4jIntegration;
using Neo4jIntegration.Models;
using Neo4jIntegration.Models.Versioning;
using Neo4jIntegration.Reflection;
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
    public class Neo4jIntegration
    {
        [TestMethod]
        public void TestInsertStyle()
        {
            Style s = new Style();
            s.Name = "Concrete";
            s.Category.Add(Category.Bags, AcceptanceState.Suggested);
            s.Category.Add(Category.Headwear, AcceptanceState.Suggested);
            s.Category.Add(Category.Legging, AcceptanceState.Suggested);
            s.WaistLine.Add(new List<float>(new float[] { 0, 1, 2.2f }), AcceptanceState.Suggested);
            Color c = new Color();
            c.Hex.Add("ABC123", AcceptanceState.Suggested);
            Color c2 = new Color();
            c2.Hex.Add("DEF987", AcceptanceState.Accepted);
            c.Template.Add(c2, AcceptanceState.RolledBack);
            s.Colors.Add(new List<Color>(new Color[] { c }), AcceptanceState.Suggested);
            s.Template = new Style()
            {
                Name = "Template",
                Template = new Style()
                {
                    Name = "Super Template"
                }
            };

            var client = /*(ITransactionalGraphClient)*/new GraphClient(new Uri("http://localhost:7474/db/data"), "neo4j", "pass");
            client.Connect();

            s.Save(client);
            
            var a = Neo4jSet<Style>.All(client).Include(x => x.Template.Template)
                //.Include<Versionable<Category>, VersionableItteration<Category>, Category>(x => x.Category, c => c.Value)
                .Include(x => x.Category)
                .ThenIncludeCollection<ReferenceCollection<VersionableItteration<Category>>, VersionableItteration<Category>, Category>
                    (x => x.Versions, x => x.Value)
                .Include(x => x.Colors)
                .ThenIncludeCollection<ReferenceCollection<VersionableItteration<ICollection<Color>>>, VersionableItteration<ICollection<Color>>, ICollection<Color>>
                    (x => x.Versions, x => x.Value)
                //.ThenIncludeCollection<ICollection<Color>, Color, Color>
                //    (x => x, x => x)
                ;
            //TODO: ReadValidate
            ReflectReadDictionary<Style>[] sOut = a.Results.ToArray();
        }
    }
}
