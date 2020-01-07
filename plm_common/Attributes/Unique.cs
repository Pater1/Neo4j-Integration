using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Attributes
{
    public class Unique : Attribute, INeo4jAttribute, IOnWriteAttribute
    {
        public virtual bool OnWrite(DependencyInjector depInj)
        {
            return false;
        }
    }
}
