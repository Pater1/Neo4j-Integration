using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo4jIntegration.Attributes
{
    public class EscapeString : Attribute, INeo4jAttribute, IOnWriteAttribute, IOnReadAttribute
    {
        public virtual void OnRead(DependencyInjector depInj)
        {
            string o = depInj.Get("value").ToString();
            depInj.Set("value", new string(o.Skip(1).Reverse().Skip(1).Reverse().ToArray()));
        }

        public virtual bool OnWrite(DependencyInjector depInj)
        {
            string o = depInj.Get("value").ToString();
            depInj.Set("value", "'" + o + "'");
            return false;
        }
    }
}
