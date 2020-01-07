using Neo4jClient;
using Neo4jClient.Transactions;
using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Attributes
{
    public class Index : Attribute, INeo4jAttribute, IOnWriteAttribute
    {
        string indexName;
        public Index(string indexName)
        {
            this.indexName = indexName;
        }

        public bool OnWrite(DependencyInjector depInj)
        {
            ITransactionalGraphClient client = depInj.Get<ITransactionalGraphClient>("GraphClient");
            if (!client.CheckIndexExists("Persons", IndexFor.Node))
                client.CreateIndex("Persons", 
                    new IndexConfiguration { Provider = IndexProvider.lucene, Type = IndexType.exact }, 
                    IndexFor.Node);
            return false;
        }
    }
}
