using CMS.API.Infrastructure.Repositories;
using CMS.API.Models.DomainModels;
using CMS.FunctionalTests.Base;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CMS.FunctionalTests
{
    public class DmnScenario : CMSScenarioBase
    {
        [Theory]
        [InlineData("Document", 118, 0, "", "Начальное состояние дела", "CreateTask56")]
        [InlineData("Task", 56, 0, "Подготовлено и направлено требование стороне", "Дело проверено", "ProcessClaim")]
        [InlineData("Portfolio", 2, 1, "Дело проверено", "Начальное состояние дела", "CreateTask56")]
        public async Task GetDecision_success(string senderType, int objectId, int resultId, string currentCondition, string nextCondition, string methodName)
        {
            using (var server = CreateServer())
            {
                var repository = server.Host.Services.GetRequiredService<ICamundaRepository>();

                var decisionInput = new DicisionInput
                {
                    SenderType = senderType,
                    ObjectId = objectId,
                    ResultId = resultId,
                    CurrentCondition = currentCondition
                };

                var result = await repository.GetDecisionResult(decisionInput);
                Assert.NotNull(result);
                Assert.Equal(nextCondition, result.NextCondition);
                Assert.Equal(methodName, result.MethodName);
            }
        }
    }
}
