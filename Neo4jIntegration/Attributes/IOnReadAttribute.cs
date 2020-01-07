using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Attributes
{
    public interface IOnReadAttribute
    {
        public void OnRead(DependencyInjector depInj);
    }
}
