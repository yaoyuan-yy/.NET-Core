using CoreAPI.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAPI.Services
{
    public class Test : ITest
    {
        public int GetId()
        {
            return 1;
        }
    }
}
