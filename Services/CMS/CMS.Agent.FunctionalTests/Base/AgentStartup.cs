using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Agent.FunctionalTests.Base;

public class AgentStartup
{
    public AgentStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }
    
    public void ConfigureServices(IServiceCollection services)
    {
        // Added to avoid the Authorize data annotation in test environment. 
        // Property "SuppressCheckForUnhandledSecurityMetadata" in appsettings.json
        //services.Configure<RouteOptions>(Configuration);
    }
    
    public void Configure(IApplicationBuilder app)
    {
    }
}