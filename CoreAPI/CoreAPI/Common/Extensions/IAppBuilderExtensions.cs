using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Model.consul;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAPI.Common.Extensions
{
    public static class IAppBuilderExtensions
    {
        public static IApplicationBuilder RegisterConsul(this IApplicationBuilder app, IHostApplicationLifetime lifetime, ServiceEntity serviceEntity)
        {
            var consulClient = new ConsulClient(x => x.Address = new Uri($"http://{serviceEntity.ConsulIP}:{serviceEntity.ConsulPort}"));
            var httpCheck = new AgentServiceCheck()
            {
                // 服务器启动之后多久注册
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                // 健康检查时间间隔,或者称为心跳间隔
                Interval = TimeSpan.FromSeconds(10),
                // 健康检查的地址
                HTTP = $"http://{serviceEntity.IP}:{serviceEntity.Port}/api/health",
                Timeout = TimeSpan.FromSeconds(5)
            };

            // 注册到consul服务
            var registration = new AgentServiceRegistration()
            {
                Checks = new[] { httpCheck },
                ID = Guid.NewGuid().ToString(),
                Name = serviceEntity.ServiceName,
                Address = serviceEntity.IP,
                Port = serviceEntity.Port,
                // 添加urlprefix-/servicename格式的tag标签，一遍Fabio识别
                Tags =new[] {$"urlprefix-/{serviceEntity.ServiceName}"}
            };

            // 服务启动时注册,内部实现其实就是使用consul API进行注册(HttpClient发起)
            consulClient.Agent.ServiceRegister(registration).Wait();
            lifetime.ApplicationStopping.Register(()=>
            {
                // 服务停止时取消注册
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            });
            return app;
        }
    }
}
