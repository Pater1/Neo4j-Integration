using System;
using System.Collections.Generic;
using System.Text;

namespace Neo4jIntegration.Attributes
{
    public interface IOnWriteAttribute { 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="depInj"></param>
        /// <returns>if the "value" key in depInj was edited</returns>
        public bool OnWrite(DependencyInjector depInj);
    }
}
