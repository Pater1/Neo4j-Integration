using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Attributes
{
    public class Unique : Attribute, INeo4jAttribute, IOnWriteAttribute
    {
        public virtual bool OnWrite(DependencyInjector depInj)
        {
            return false;
        }
    }
}
