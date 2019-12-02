using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                    // ����Apollo����
                    .ConfigureAppConfiguration((builder)=> {
                        var configuration = builder.Build();

                        var isUseApollo = configuration.GetValue("Apollo:UseApolloConfigCenter", false);

                        if (isUseApollo)
                        {
                            builder.AddApollo(builder.Build().GetSection("Apollo"))
                            .AddDefault()
                            .AddNamespace("TEST1.Consul");
                        }
                    })
                    ;
                });
    }
}
