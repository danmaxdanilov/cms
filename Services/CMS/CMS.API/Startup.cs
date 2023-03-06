using Autofac;
using Autofac.Extensions.DependencyInjection;
using CMS.API.Controllers;
using CMS.API.Infrastructure.Filters;
using CMS.API.Infrastructure.Middlewares;
using CMS.API.Infrastructure.Repositories;
using CMS.API.Infrastructure.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using CMS.API.CommandHandlers;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Commands;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CMS.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
                options.Filters.Add(typeof(ValidateModelStateFilter));

            }) // Added for functional tests
                .AddApplicationPart(typeof(CMSController).Assembly)
                .AddNewtonsoftJson();

            ConfigureSwaggerService(services);

            ConfigureAuthService(services);

            services.AddCustomHealthCheck(Configuration);

            services.Configure<CMSSettings>(Configuration);

            services.AddSingleton<IMapper>(
                provider => new Mapper(MapperConfig.CreateConfig()));

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                    .SetIsOriginAllowed((host) => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            ConfigureDatabase(services);
            
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IIdentityService, IdentityService>();
            services.AddTransient<IEntryRepository, EntryRepository>();
            services.AddTransient<ICommandRepository, CommandRepository>();
            services.AddTransient<IEntryService, EntryService>();

            services.AddOptions();

            ConfigureKafka(services);

            var container = new ContainerBuilder();
            //container.AddKafka(Configuration["Kafka:BootstrapServers"], Configuration["Kafka:GroupId"]);
            //container.AddKafkaProducer<string, AddEntryCommand>();
            container.Populate(services);
            container.Build();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var pathBase = Configuration["PATH_BASE"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            ConfigureSwagger(app, pathBase);
            
            app.UseRouting();
            app.UseCors("CorsPolicy");
            ConfigureAuth(app);

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });

            //ConfigureEventBus(app);
        }

        private void ConfigureAuthService(IServiceCollection services)
        {
            // prevent from mapping "sub" claim to nameidentifier.
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

            var identityUrl = Configuration.GetValue<string>("IdentityUrl");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = identityUrl;
                options.RequireHttpsMetadata = false;
                options.Audience = "CMS";
            });
        }

        protected virtual void ConfigureAuth(IApplicationBuilder app)
        {
            if (Configuration.GetValue<bool>("UseLoadTest"))
            {
                app.UseMiddleware<ByPassAuthMiddleware>();
            }

            app.UseAuthentication();
            app.UseAuthorization();
        }

        
        
        protected virtual void ConfigureDatabase(IServiceCollection services)
        {
            services.AddNpgsql<PgDbContext>(
                Configuration["ConnectionStrings:PostgreSQL"],
                opt => opt.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "cms_schema"));
        }
        
        protected virtual void ConfigureSwaggerService(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Juridical Robot - CMS HTTP API",
                    Version = "v1",
                    Description = "The CMS Service HTTP API"
                });

                /*options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows()
                    {
                        Implicit = new OpenApiOAuthFlow()
                        {
                            AuthorizationUrl = new Uri($"{Configuration.GetValue<string>("IdentityUrlExternal")}/connect/authorize"),
                            TokenUrl = new Uri($"{Configuration.GetValue<string>("IdentityUrlExternal")}/connect/token"),
                            Scopes = new Dictionary<string, string>()
                            {
                                { "CMS", "CMS API" }
                            }
                        }
                    }
                });*/

                options.OperationFilter<AuthorizeCheckOperationFilter>();
            });
        }

        protected virtual void ConfigureSwagger(IApplicationBuilder app, string pathBase)
        {
            app.UseSwagger()
                .UseSwaggerUI(setup =>
                {
                    setup.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "CMS.API V1");
                    setup.OAuthClientId("CMSswaggerui");
                    setup.OAuthAppName("CMS Swagger UI");
                });
        }

        protected virtual void ConfigureKafka(IServiceCollection services)
        {
            services.AddKafka(Configuration["Kafka:BootstrapServers"], Configuration["Kafka:GroupId"]);
            services.AddKafkaProducer<string, AddEntryCommand>();
            services.AddHostedService<AddEntryCommandResponseHandler>();
        }
    }

    public static class CustomExtensionMethods
    {
        public static IServiceCollection AddCustomHealthCheck(this IServiceCollection services, IConfiguration configuration)
        {
            var hcBuilder = services.AddHealthChecks();

            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());

            hcBuilder
                .AddSqlServer(
                    configuration["ConnectionString"],
                    name: "database-check",
                    tags: new string[] { "redis" });
            

            return services;
        }
    }
}
