using Microsoft.IdentityModel.Tokens;
using Model.Auth;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CoreAPI.Common.AuthHelper
{
    public class JwtHelper
    {
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string issueJwt(TokenModelJwt token)
        {
            var iss = Startup.Issuer;
            var aud = Startup.Audience;
            var secret = Startup.Secret;
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti,token.Uid.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,$"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}"),
                new Claim(JwtRegisteredClaimNames.Nbf,$"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}"),
                // 过期时间
                new Claim(JwtRegisteredClaimNames.Exp,$"{new DateTimeOffset(DateTime.Now.AddSeconds(1000)).ToUnixTimeSeconds()}"),
                new Claim(JwtRegisteredClaimNames.Iss,iss),
                new Claim(JwtRegisteredClaimNames.Aud,aud)
            };
            // 添加一个用户的多个角色
            claims.AddRange(token.Role.Split(',').Select(s=>new Claim(ClaimTypes.Role,s)));

            // 秘钥,对安全性的要求，秘钥的长度太短会报出异常
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer:iss,
                claims:claims,
                signingCredentials:creds
                );

            var jwtHandler = new JwtSecurityTokenHandler();
            var encodeJwt = jwtHandler.WriteToken(jwt);

            return encodeJwt;
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="jwtStr"></param>
        /// <returns></returns>
        public static TokenModelJwt SerializeJwt(string jwtStr)
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(jwtStr);
            object role;
            try
            {
                jwtToken.Payload.TryGetValue(ClaimTypes.Role,out role);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
            var tm = new TokenModelJwt()
            {
                Uid = long.Parse(jwtToken.Id),
                Role = role != null ? role.ToString() : ""
            };
            return tm;
        }
    }
}
