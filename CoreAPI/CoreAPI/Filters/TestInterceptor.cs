using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAPI.Filters
{
    public class TestInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Console.WriteLine($"你正在调用方法{invocation.Method.Name},参数是{string.Join(", ",invocation.Arguments.Select(a=>(a?? "").ToString().ToArray()))}");
            invocation.Proceed();
            Console.WriteLine($"方法执行完毕,返回结果是:{invocation.ReturnValue}");
        }
    }
}
