﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using plm_common.DB;

namespace plm_common.Attributes
{
    public class StoreDirectAttribute : Attribute, INeo4jAttribute, ICustomDBSchema
    {
        ReadQueryParams<T> ICustomDBSchema.ReadValue<T>(ReadQueryParams<T> rParams)
        {
            return rParams;
        }

        SaveQuearyParams<T> ICustomDBSchema.SaveValue<T>(SaveQuearyParams<T> qParams)
        {
            object paramValue = qParams.prop.WriteValidate(qParams.depInj, qParams.buildFor.backingInstance);

            return DBOps<T>.Writes<T>.WriteSingleInline(qParams, qParams.prop.Name, paramValue);
        }
    }
}
