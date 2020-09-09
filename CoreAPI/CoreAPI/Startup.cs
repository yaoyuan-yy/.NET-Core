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

            #region ����
            // ��ȡ�����ļ�
            var keyByteArray = Encoding.ASCII.GetBytes(Secret);
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            #endregion

            // �����֤
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
                    // TokenValidated����Token��֤ͨ������ã�AuthenticationFailed:��֤ʧ��ʱ���ã�Challenge:δ��Ȩʱ����
                };

                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    // ������
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    // ������
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true
                };
            });

            // �����Ȩ����
            services.AddAuthorization(options=> {
                // ������ɫ
                options.AddPolicy("Client", policy => policy.RequireRole("Client").Build());
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
                // ��Ĺ�ϵ
                options.AddPolicy("SystemOrAdmin",policy=>policy.RequireRole("Admin","System"));
                // �ҵĹ�ϵ
                options.AddPolicy("SystemcAndAdmin",policy=>policy.RequireRole("Admin").RequireRole("System"));

            });



            // ����SessionStore,���Խ�Cookie�洢�ڷ����
            services.AddOptions<CookieAuthenticationOptions>("Cookies")
                .Configure<ITicketStore>((o,t)=>o.SessionStore=t);

            services.AddControllers();

            //ע��Swagger������������һ���Ͷ��Swagger �ĵ�
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                // ΪSwagger����xml�ĵ�ע��·��
                string apiXmlPath = Path.Combine(basePath,"CoreAPI.xml");
                c.IncludeXmlComments(apiXmlPath);
                string modelXmlPath = Path.Combine(basePath,"Model.xml");
                c.IncludeXmlComments(modelXmlPath);

                #region Token�󶨵�ConfigureServices

                // ������ȨС��
                c.OperationFilter<AddResponseHeadersFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

                // ��header�����token,���ݵ���̨
                c.OperationFilter<SecurityRequirementsOperationFilter>();

                // ������oauth2
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "JWT��Ȩ(���ݽ�������ͷ�н��д���)ֱ�����¿�������Bearer {token}(ע������֮����һ���ո�)",
                    // jwt��Ĭ�ϵĲ�������
                    Name = "Authorization",
                    // jwtĬ�ϴ��Authorization��Ϣ��λ��(����ͷ��)
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                #endregion
            });

            services.AddTransient<IControllerActivator>();
            services.AddScoped<IControllerActivator>();
            services.AddSingleton<IControllerActivator>();


            // ע��ServiceEntityʵ����
            services.AddServiceEntity(Configuration);

            // ��������ע��
            services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
        }
        /// <summary>
        /// Auotfacע��
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            // ��ע��������(������ע��Ҫ��ʹ���������Ľӿں�����֮ǰ,��������ʹ��,��virtual�������Դ���������)
            builder.RegisterType<TestInterceptor>().InstancePerDependency();

            builder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .AsImplementedInterfaces()
                .EnableInterfaceInterceptors();

            // ��ȡ���п���������
            var controllerBaseType = typeof(ControllerBase);
            builder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .Where(t => controllerBaseType.IsAssignableFrom(t) && t != controllerBaseType)
                // ��������ע��
                .PropertiesAutowired()
                // ������Controller����ʹ��������
                .EnableClassInterceptors();

            // ע����
            builder.RegisterType<TopicService>();
            // ע��ӿ�
            builder.RegisterType<Test>().As<ITest>();

            // ע��Ҫͨ�����䴴�������
            // builder.RegisterType<Repository>().As<IServices>();

            // ע����򼯣������ʵ���࣬���ǽӿڲ㣩
            // string assemblePath = Path.Combine(basePath, "Repository.dll");
            // var assemblyService = Assembly.LoadFile(assemblePath);
            // ָ����ɨ������е�����ע��Ϊ�ṩ������ʵ�ֵĽӿ�
            // builder.RegisterAssemblyTypes(assemblyService).AsImplementedInterfaces();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment  env,IHostLifetime lifetime, Model.consul.ServiceEntity serviceEntity, TopicService topic)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                //�����м����������Swagger��ΪJSON�ս��
                app.UseSwagger();
                //�����м�������swagger-ui��ָ��Swagger JSON�ս��
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                    // ���ø��ڵ����
                    c.RoutePrefix = string.Empty;
                });
            }
            string i = topic.GetId();
            app.UseRouting();
            // ��֤�м��
            app.UseAuthentication();
            // ��Ȩ�м��
            app.UseAuthorization();
            // �쳣�м��,Ҫ�ŵ����
            // app.UseExceptionHandler();

            #region consul
            // app.RegisterConsul(lifetime, serviceEntity);
            #endregion

            #region �Զ����м��
            // ��¼http���� ���롢���ֵ
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
