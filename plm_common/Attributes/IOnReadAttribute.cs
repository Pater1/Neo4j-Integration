using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Attributes
{
    public interface IOnReadAttribute
    {
        public void OnRead(DependencyInjector depInj);
    }
}
