using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Model.consul;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAPI.Common.Extensions
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddServiceEntity(this IServiceCollection services, IConfiguration configuration)
        {
            ServiceEntity entity = new ServiceEntity();
            try
            {
                entity.IP = configuration.GetValue<string>("IP");
                entity.Port = configuration.GetValue<Int32>("Port");
                entity.ServiceName = configuration.GetValue<string>("ServiceName");
                entity.ConsulIP = configuration.GetValue<string>("ConsulIP");
                entity.ConsulPort = configuration.GetValue<Int32>("ConsulPort");
            }
            catch (Exception)
            {
                entity = new ServiceEntity();
            }
            services.AddSingleton(entity);
            return services;
        }
    }
}
