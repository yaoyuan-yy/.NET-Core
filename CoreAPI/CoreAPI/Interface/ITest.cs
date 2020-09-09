using Autofac.Extras.DynamicProxy;
using CoreAPI.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAPI.Interface
{
    [Intercept(typeof(TestInterceptor))]
    public interface ITest
    {
        int GetId();
        void DefaultImpl(int i) => DefaultImpl(1);
    }
}
