﻿using CMS.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
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

        protected override void ConfigureKafka(IServiceCollection serviceCollection)
        {
            //disable kafka for tests
            serviceCollection.AddKafka(Configuration["Kafka:BootstrapServers"], Configuration["Kafka:GroupId"]);
            serviceCollection.AddKafkaProducer<string, AddEntryCommand>();
            //base.ConfigureKafka(serviceCollection);
        }
    }
}