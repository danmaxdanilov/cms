using Camunda.Api.Client;
using Camunda.Api.Client.DecisionDefinition;
using CMS.API.Models.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CMS.API.Infrastructure.Repositories
{
    public interface ICamundaRepository
    {
        Task<DicisionOutput> GetDecisionResult(DicisionInput input);
    }

    public class CamundaRepository : ICamundaRepository
    {
        private readonly CamundaContext _context;
        public CamundaRepository(CamundaContext context)
        {
            _context = context;
        }

        public async Task<DicisionOutput> GetDecisionResult(DicisionInput input)
        {
            var evaluateDecision = new EvaluateDecision
            {
                Variables = new Dictionary<string, VariableValue>()
            };
            evaluateDecision.Variables.Add("senderType", VariableValue.FromObject(input.SenderType));
            evaluateDecision.Variables.Add("objectId", VariableValue.FromObject(input.ObjectId));
            evaluateDecision.Variables.Add("resultId", VariableValue.FromObject(input.ResultId));
            evaluateDecision.Variables.Add("currentCondition", VariableValue.FromObject(input.CurrentCondition));

            var output = new DicisionOutput();
            try
            {
                var conditionDecision = (await _context.CamundaClient.DecisionDefinitions
                        .ByKey("CMS-conditions")
                        .Evaluate(evaluateDecision)).SingleOrDefault();
                output.NextCondition = conditionDecision.ContainsKey("nextCondition") ? conditionDecision["nextCondition"].GetValue<string>() : String.Empty;
                output.MethodName = conditionDecision.ContainsKey("methodName") ? conditionDecision["methodName"].GetValue<string>() : String.Empty;
            }
            catch (Exception e)
            {
                throw new Exception($"Обнаружено запрещенное состояние! senderType: {input.SenderType}, objectId: {input.ObjectId}");
            }

            return output;
        }
    }

    public class CamundaContext
    {
        private readonly CamundaClient _camundaClient;
        public CamundaContext(string url, string token)
        {
            var authValue = new AuthenticationHeaderValue("Bearer", token);
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };
            httpClient.DefaultRequestHeaders.Authorization = authValue;
            _camundaClient = CamundaClient.Create(httpClient);
        }

        public CamundaClient CamundaClient => _camundaClient ?? null;
    }
}
