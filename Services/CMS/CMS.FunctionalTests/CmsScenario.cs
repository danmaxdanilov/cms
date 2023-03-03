using CMS.FunctionalTests.Base;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CMS.API.Infrastructure.Repositories;
using CMS.API.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CMS.FunctionalTests
{
    public class CMSScenario : CMSScenarioBase
    {
        [Fact]
        public async Task GetEntryList_Success()
        {
            using (var server = CreateServer())
            {
                var context = server.Host.Services.GetRequiredService<PgDbContext>();
                var logger = server.Host.Services.GetRequiredService<ILogger<PgDbContext>>();
                    
                await PgDbContextSeed.SeedDatabaseAsync(context, logger);
                
                var pageIndex = 0;
                var pageSize = 15;
                var param = new Dictionary<string, string>()
                {
                    { "entryName", "mc" },
                    { "entryVersion", "1.27"},
                    {nameof(pageIndex), $"{pageIndex}"},
                    {nameof(pageSize), $"{pageSize}"},
                };
                var response = await server.CreateClient()
                   .GetAsync(Get.GetEntryList(param));

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                Assert.False(string.IsNullOrEmpty(responseBody));
                var entryList = JsonConvert.DeserializeObject<PaginatedItemsViewModel<EntryItem>>(responseBody);
                Assert.NotNull(entryList);
                Assert.Equal(pageIndex, entryList.PageIndex);
                Assert.Equal(pageSize, entryList.PageSize);
            }
        }
        
        [Fact]
        public async Task AddEntry_Success()
        {
            using (var server = CreateServer())
            {
                var context = server.Host.Services.GetRequiredService<PgDbContext>();
                var logger = server.Host.Services.GetRequiredService<ILogger<PgDbContext>>();
                    
                await PgDbContextSeed.SeedDatabaseAsync(context, logger);

                var name = "mc";
                var entryInDb = await context.Entries.Where(x => x.Name == name).OrderBy(x => x.Version).LastOrDefaultAsync();
                var version = entryInDb == null ? "1.0" : entryInDb.Version + ".2";
                
                var content = new StringContent(BuildEntryRequest(name, version), UTF8Encoding.UTF8, "application/json");
                
                var response = await server.CreateClient()
                    .PutAsync(Put.AddEntry, content);

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                Assert.False(string.IsNullOrEmpty(responseBody));
                var entry = JsonConvert.DeserializeObject<EntryItem>(responseBody);
                Assert.NotNull(entry);
                Assert.Equal(name, entry.Name);
                Assert.Equal(version, entry.Version);
            }
        }


        string BuildEntryRequest(string name, string version)
        {
            var requestItem = new EntryRequest
            {
                Name = name,
                Version = version
            };
            
            return JsonSerializer.Serialize(requestItem);
        }
    }
}
