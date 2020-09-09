using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Autofac.Extras.DynamicProxy;
using CoreAPI.Filters;
using CoreAPI.Interface;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Model;
using Model.consul;
using MySql.Data.MySqlClient;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Intercept(typeof(TestInterceptor))]
    public class TestController : ControllerBase
    {
        public ITest test { get; set; }

        public ServiceEntity serviceEntity { get; set; }
        public TestController(ITest test, ServiceEntity serviceEntity)
        {
            this.test = test;
            this.serviceEntity = serviceEntity;
        }

        [HttpPost("test")]
        [Authorize(Policy ="SystemAndAdmin")]
        public void Test([FromBody]TestDTO test)
        {

        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <returns></returns>
        [HttpPost("add")]
        // [Authorize(Roles ="Admin")]
        public string Add()
        {
            var content = new Content() { 
            title="标题1",
            content="内容1",
            };
            var result = 0;
            using (var conn=new MySqlConnection("Data Source=192.168.187.11;User Id=root;Password=153246;Database=CoreDemeDB;Pooling=true;Max Pool Size=100"))
            {
                string sqlInsert = "INSERT INTO content(title,content,status,addTime,modifyTime) VALUES(@title,@content,@status,@addTime,@modifyTime)";
                try
                {
                    result = conn.Execute(sqlInsert, content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return $"插入了{result}条数据!";


            // return test.GetId().ToString();
            // return "";
        }
    }
}