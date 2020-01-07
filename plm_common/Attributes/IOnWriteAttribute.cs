using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Attributes
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
