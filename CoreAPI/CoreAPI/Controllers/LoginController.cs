using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CoreAPI.Common.AuthHelper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Model.Auth;

namespace CoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        /// <summary>
        /// 登录
        /// </summary>
        [HttpGet]
        [HttpPost]
        public async Task<object> LoginIn(string name,string password)
        {
            // 将用户Id 和角色名，作为单独的自定义变量封装进Token字符串中
            TokenModelJwt tokenModel = new TokenModelJwt
            {
                Uid=1,
                Role="Admin"
            };
            // 登录,获取到一定规则的Token令牌
            var jwtStr = JwtHelper.issueJwt(tokenModel);
            var suc = true;
            return Ok(new { success=suc,token=jwtStr});
        }
    }
}