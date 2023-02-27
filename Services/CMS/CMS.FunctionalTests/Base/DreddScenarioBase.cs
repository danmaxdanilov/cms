using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace CMS.FunctionalTests.Base
{
    public class CMSScenarioBase
    {
        private const string ApiUrlBase = "api/v1/CMS";

        public TestServer CreateServer()
        {
            var path = Assembly.GetAssembly(typeof(CMSScenarioBase))
               .Location;

            var hostBuilder = new WebHostBuilder()
                .UseContentRoot(Path.GetDirectoryName(path))
                .ConfigureAppConfiguration(cb =>
                {
                    cb.AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables();
                }).UseStartup<CMSTestStartup>();

            return new TestServer(hostBuilder);
        }

        public static class Get
        {
            public static string GetTestNumber()
            {
                return $"{ApiUrlBase}";
            }
        }

        public static class Post
        {
            public static string CMS = $"{ApiUrlBase}/";
        }
    }
}
