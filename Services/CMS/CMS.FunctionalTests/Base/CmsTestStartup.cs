using CMS.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using CMS.API.Infrastructure.Repositories;
using CMS.Shared.Kafka;
using CMS.Shared.Kafka.Commands;

namespace CMS.FunctionalTests.Base
{
    class CMSTestStartup : Startup
    {
        public CMSTestStartup(IConfiguration env) : base(env)
        {
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            // Added to avoid the Authorize data annotation in test environment. 
            // Property "SuppressCheckForUnhandledSecurityMetadata" in appsettings.json
            services.Configure<RouteOptions>(Configuration);
            base.ConfigureServices(services);
        }

        protected override void ConfigureAuth(IApplicationBuilder app)
        {
            if (Configuration["isTest"] == bool.TrueString.ToLowerInvariant())
            {
                app.UseMiddleware<AutoAuthorizeMiddleware>();
            }
            else
            {
                base.ConfigureAuth(app);
            }
        }

        protected override void ConfigureKafka(IServiceCollection services)
        {
            //disable kafka for tests
            services.AddKafka(Configuration["Kafka:BootstrapServers"], Configuration["Kafka:GroupId"]);
            services.AddKafkaProducer<string, AddEntryCommand>();
            services.AddKafkaProducer<string, RemoveEntryCommand>();
            //base.ConfigureKafka(serviceCollection);
        }

        protected override void ConfigureSwagger(IApplicationBuilder app, string pathBase)
        {
            //skip for tests
        }

        protected override void ConfigureSwaggerService(IServiceCollection services)
        {
            //skip for tests
        }

        protected override void ConfigureDatabase(IServiceCollection services)
        {
            //test required
            //use real database
            base.ConfigureDatabase(services);
        }
    }
}
