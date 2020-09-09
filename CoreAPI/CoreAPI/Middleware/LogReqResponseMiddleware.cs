using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CoreAPI.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class LogReqResponseMiddleware
    {
        private readonly RequestDelegate _next;

        public LogReqResponseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// 将请求传递给下一个中间件
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext httpContext, ILogger<LogReqResponseMiddleware> logger)
        {
            var request = httpContext.Request;
            request.EnableBuffering();
            // 把请求body流转换成字符串
            string bodyAsText = await new StreamReader(request.Body).ReadToEndAsync();
            var requestStr = $"{request.Scheme} {request.Host}{request.Path}{request.QueryString}{bodyAsText}";
            logger.LogDebug("Request:" + requestStr);
            request.Body.Seek(0,SeekOrigin.Begin);

            var originalBodyStream = httpContext.Response.Body;
            using (var responseBody=new MemoryStream())
            {
                httpContext.Response.Body = responseBody;
                await _next(httpContext);

                var response = httpContext.Response;
                response.Body.Seek(0,SeekOrigin.Begin);
                // 转化为字符串
                string text = await new StreamReader(response.Body).ReadToEndAsync();
                // 从新设置偏移量0
                response.Body.Seek(0,SeekOrigin.Begin);

                // 记录返回值
                var responsestr = $"{response.StatusCode}:{text}";
                logger.LogDebug("Response:"+responsestr);

                await responseBody.CopyToAsync(originalBodyStream);
            }
            // return _next(httpContext);
        }
    }

    /// <summary>
    /// 将自定义中间件添加到Http请求管道的扩展方法
    /// </summary>
    public static class LogReqResponseMiddlewareExtensions
    {
        public static IApplicationBuilder UseLogReqResponseMiddleware(this IApplicationBuilder builder, object p)
        {
            return builder.UseMiddleware<LogReqResponseMiddleware>();
        }
    }
}
