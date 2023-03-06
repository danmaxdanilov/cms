using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.WebUtilities;

namespace CMS.FunctionalTests.Base
{
    public class CMSScenarioBase
    {
        private const string ApiUrlBase = "api/v1";

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
            public static string GetEntryList(Dictionary<string,string> @params)
            {
                return QueryHelpers.AddQueryString($"{ApiUrlBase}/entry/list?", @params);
            }
        }

        public static class Post
        {
            public static string RemoveEntry = $"{ApiUrlBase}/entry/remove";
        }
        
        public static class Put
        {
            public static string AddEntry = $"{ApiUrlBase}/entry/add";
        }
    }
}
