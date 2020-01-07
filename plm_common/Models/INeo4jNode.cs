using System;
using System.Collections.Generic;
using System.Text;

namespace plm_common.Models
{
    public interface INeo4jNode
    {
        public string Id { get; }
        bool IsActive { get; }
    }
}
