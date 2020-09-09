using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Com.Ctrip.Framework.Apollo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoreAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            // 使用默认的配置信息来初始化一个IHostBuilder实例
            Host.CreateDefaultBuilder(args)
                // Autofac
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                // 使用默认的配置信息来初始化一个IWebHostBuilder
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                    // 加载Apollo配置
                    .ConfigureAppConfiguration((hostBuildContext,config) =>
                    {
                        var env = hostBuildContext.HostingEnvironment;

                        config.AddJsonFile("app.json", false, true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", false, true);

                        var configuration = config.Build();
                        var isUseApollo = configuration.GetValue("Apollo:UseApolloConfigCenter", false);
                        if (isUseApollo)
                        {
                            config.AddApollo(config.Build().GetSection("Apollo"))
                            .AddDefault()
                            .AddNamespace("TEST1.public")
                            .AddNamespace("TEST1.Consul");
                        }
                    });
                });
    }
}
