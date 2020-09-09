using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.DynamicProxy;
using CoreAPI.Common.Extensions;
using CoreAPI.Filters;
using CoreAPI.Interface;
using CoreAPI.Middleware;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Model;
using Model.consul;
using Swashbuckle.AspNetCore.Filters;

namespace CoreAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            basePath = AppDomain.CurrentDomain.BaseDirectory;

            Issuer = Configuration.GetValue<string>("Audience.Issuer");
            Audience = Configuration.GetValue<string>("Audience.Audience");
            Secret = Configuration.GetValue<string>("Audience.Secret");
        }

        public string basePath;
        public IConfiguration Configuration { get; }

        public static string Issuer = "";
        public static string Audience = "";
        public static string Secret = "";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ServiceEntity>(Configuration.GetSection("ServiceEntity"));

            #region 参数
            // 读取配置文件
            var keyByteArray = Encoding.ASCII.GetBytes(Secret);
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            #endregion

            // 添加认证
            services.AddAuthentication(authOpt =>
            {
                authOpt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authOpt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(option=> {
                option.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Query["access_token"];
                        return Task.CompletedTask;
                    }
                    // TokenValidated：在Token验证通过后调用，AuthenticationFailed:认证失败时调用，Challenge:未授权时调用
                };

                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    // 发行人
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    // 订阅人
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true
                };
            });

            // 添加授权策略
            services.AddAuthorization(options=> {
                // 单独角色
                options.AddPolicy("Client", policy => policy.RequireRole("Client").Build());
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
                // 或的关系
                options.AddPolicy("SystemOrAdmin",policy=>policy.RequireRole("Admin","System"));
                // 且的关系
                options.AddPolicy("SystemcAndAdmin",policy=>policy.RequireRole("Admin").RequireRole("System"));

            });



            // 开启SessionStore,可以将Cookie存储在服务端
            services.AddOptions<CookieAuthenticationOptions>("Cookies")
                .Configure<ITicketStore>((o,t)=>o.SessionStore=t);

            services.AddControllers();

            //注册Swagger生成器，定义一个和多个Swagger 文档
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                // 为Swagger设置xml文档注释路径
                string apiXmlPath = Path.Combine(basePath,"CoreAPI.xml");
                c.IncludeXmlComments(apiXmlPath);
                string modelXmlPath = Path.Combine(basePath,"Model.xml");
                c.IncludeXmlComments(modelXmlPath);

                #region Token绑定到ConfigureServices

                // 开启加权小锁
                c.OperationFilter<AddResponseHeadersFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

                // 在header中添加token,传递到后台
                c.OperationFilter<SecurityRequirementsOperationFilter>();

                // 必须是oauth2
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "JWT授权(数据将在请求头中进行传输)直接在下框中输入Bearer {token}(注意两者之间是一个空格)",
                    // jwt的默认的参数名称
                    Name = "Authorization",
                    // jwt默认存放Authorization信息的位置(请求头中)
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                #endregion
            });

            services.AddTransient<IControllerActivator>();
            services.AddScoped<IControllerActivator>();
            services.AddSingleton<IControllerActivator>();


            // 注入ServiceEntity实体类
            services.AddServiceEntity(Configuration);

            // 允许属性注入
            services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
        }
        /// <summary>
        /// Auotfac注入
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            // 先注册拦截器(拦截器注册要在使用拦截器的接口和类型之前,在类型中使用,仅virtual方法可以触发拦截器)
            builder.RegisterType<TestInterceptor>().InstancePerDependency();

            builder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .AsImplementedInterfaces()
                .EnableInterfaceInterceptors();

            // 获取所有控制器类型
            var controllerBaseType = typeof(ControllerBase);
            builder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .Where(t => controllerBaseType.IsAssignableFrom(t) && t != controllerBaseType)
                // 允许属性注入
                .PropertiesAutowired()
                // 允许在Controller类上使用拦截器
                .EnableClassInterceptors();

            // 注入类
            builder.RegisterType<TopicService>();
            // 注入接口
            builder.RegisterType<Test>().As<ITest>();

            // 注册要通过反射创建的组件
            // builder.RegisterType<Repository>().As<IServices>();

            // 注册程序集（这个是实现类，不是接口层）
            // string assemblePath = Path.Combine(basePath, "Repository.dll");
            // var assemblyService = Assembly.LoadFile(assemblePath);
            // 指定已扫描程序集中的类型注册为提供所有其实现的接口
            // builder.RegisterAssemblyTypes(assemblyService).AsImplementedInterfaces();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment  env,IHostLifetime lifetime, Model.consul.ServiceEntity serviceEntity, TopicService topic)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                //启用中间件服务生成Swagger作为JSON终结点
                app.UseSwagger();
                //启用中间件服务对swagger-ui，指定Swagger JSON终结点
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                    // 设置根节点访问
                    c.RoutePrefix = string.Empty;
                });
            }
            string i = topic.GetId();
            app.UseRouting();
            // 认证中间件
            app.UseAuthentication();
            // 授权中间件
            app.UseAuthorization();
            // 异常中间件,要放到最后
            // app.UseExceptionHandler();

            #region consul
            // app.RegisterConsul(lifetime, serviceEntity);
            #endregion

            #region 自定义中间件
            // 记录http请求 输入、输出值
            //app.UseLogReqResponseMiddleware(async (context, ILogger) => {
            //    context.invoke(ILogger);
            //});

            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
            #endregion

            app.UseEndpoints(endpoints =>
            {
                // endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "api/{controller=Test}/{action=Post}/{id?}"
                    );
            });
        }
    }
}
