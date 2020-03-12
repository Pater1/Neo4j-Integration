using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver.V1;
using Neo4jClient;
using Neo4jClient.Cypher;
using Neo4jClient.Transactions;
using Neo4jIntegration;
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
        public void TestDeserialize()
        {
            string json = @"
{
  ""template"": [
    {
                ""template"": [
                  {
          ""colors_versions"": [
                      {
              ""_type"": ""Versionable:Color"",
                        ""_id"": 80,
                        ""Id"": ""90q8WC8MYi"",
                        ""IsActive"": true
            }
          ],
          ""IsActive"": true,
          ""_type"": ""Style"",
          ""_id"": 40,
          ""Id"": ""14lW3jveCb"",
          ""category_versions"": [
            {
              ""_type"": ""Versionable:Category"",
              ""_id"": 81,
              ""Id"": ""7C0bxBmM6Y"",
              ""IsActive"": true
            }
          ],
          ""Name"": ""Super Template""
        }
      ],
      ""colors_versions"": [
        {
          ""_type"": ""Versionable:Color"",
          ""_id"": 83,
          ""Id"": ""eD5TJ0JwhM"",
          ""IsActive"": true
        }
      ],
      ""IsActive"": true,
      ""_type"": ""Style"",
      ""_id"": 20,
      ""Id"": ""U8N6UJtfoy"",
      ""category_versions"": [
        {
          ""_type"": ""Versionable:Category"",
          ""_id"": 84,
          ""Id"": ""9zeVqImElJ"",
          ""IsActive"": true
        }
      ],
      ""Name"": ""Template""
    }
  ],
  ""colors_versions"": [
    {
      ""_type"": ""Versionable:Color"",
      ""item"": [
        {
          ""AcceptanceState"": 1,
          ""ItteratedTime"": ""2020-03-06T22:40:42.2308351Z"",
          ""item"": [
            {
              ""_type"": ""Color"",
              ""hex"": [
                {
                  ""_type"": ""Versionable:String"",
                  ""_id"": 90,
                  ""Id"": ""lHd3TPtJCD"",
                  ""IsActive"": true
                }
              ],
              ""_id"": 89,
              ""Id"": ""M1eImP3WPH"",
              ""IsActive"": true
            }
          ],
          ""IsActive"": true,
          ""_type"": ""Color:VersionableItteration"",
          ""_id"": 88,
          ""Id"": ""jhmVzy1muE""
        }
      ],
      ""_id"": 87,
      ""Id"": ""ajJTP7xNQR"",
      ""IsActive"": true
    }
  ],
  ""IsActive"": true,
  ""_type"": ""Style"",
  ""_id"": 0,
  ""Id"": ""3Q8sLl1Q6d"",
  ""category_versions"": [
    {
      ""_type"": ""Versionable:Category"",
      ""item"": [
        {
          ""AcceptanceState"": 1,
          ""ItteratedTime"": ""2020-03-06T22:40:42.2285544Z"",
          ""IsActive"": true,
          ""_type"": ""Category:VersionableItteration"",
          ""InlineValue"": ""16"",
          ""_id"": 97,
          ""Id"": ""aIWg2A1PBt""
        },
        {
          ""AcceptanceState"": 1,
          ""ItteratedTime"": ""2020-03-06T22:40:42.2277967Z"",
          ""IsActive"": true,
          ""_type"": ""Category:VersionableItteration"",
          ""InlineValue"": ""4"",
          ""_id"": 96,
          ""Id"": ""lv88OZfg5p""
        },
        {
          ""AcceptanceState"": 1,
          ""ItteratedTime"": ""2020-03-06T22:40:42.2133754Z"",
          ""IsActive"": true,
          ""_type"": ""Category:VersionableItteration"",
          ""InlineValue"": ""1"",
          ""_id"": 95,
          ""Id"": ""fo7AOf1vfh""
        }
      ],
      ""_id"": 94,
      ""Id"": ""BMJQq9DHEE"",
      ""IsActive"": true
    }
  ],
  ""Name"": ""Concrete""
}";

            dynamic s = JsonConvert.DeserializeObject<dynamic>(json);
        }
        [TestMethod]
        public void TestInsertStyle()
        {
            Style s = new Style();
            s.Id = (new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)).ToString("n");
            s.Name = "Concrete";
            s.Category.Add(Category.Bags, AcceptanceState.Suggested);
            s.Category.Add(Category.Headwear, AcceptanceState.Suggested);
            s.Category.Add(Category.Legging, AcceptanceState.Suggested);
            s.WaistLine.Add(new List<float>(new float[] { 0, 1, 2.2f }), AcceptanceState.Suggested);
            Color c = new Color();
            c.Id = (new Guid(100,99,98,97,96,95,94,93,92,91,90)).ToString("n");
            c.Hex.Add("ABC123", AcceptanceState.Suggested);
            Color c2 = new Color();
            c2.Id = (new Guid(50,49,48,47,46,45,44,43,42,41,40)).ToString("n");
            c2.Hex.Add("DEF987", AcceptanceState.Accepted);
            c.Template.Add(c2, AcceptanceState.RolledBack);
            s.Colors.Add(new List<Color>(new Color[] { c }), AcceptanceState.Suggested);
            s.Template = new Style()
            {
                Name = "Template",
                Id = (new Guid(10,11,12,13,14,15,16,17,18,19,20)).ToString("n"),
                Template = new Style()
                {
                    Id = (new Guid(20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30)).ToString("n"),
                    Name = "Super Template"
                }
            };

            var client = /*(ITransactionalGraphClient)*/new GraphClient(new Uri("http://localhost:7474/db/data"), "neo4j", "password");
            client.Connect();

            s.Save(client);

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

                .Include(x => x.Colors)
                .ThenInclude(x => x.Start)
                .ThenInclude(x => x.Next)
                .ThenInclude(x => x.Next)
                .ThenInclude(x => x.Value)
                
                //.Where(x => x.Id == (new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)).ToString("n"));
                ;
            //TODO: ReadValidate
            ReflectReadDictionary<Style>[] sOut = a.Results.ToArray();
        }
    }
}
