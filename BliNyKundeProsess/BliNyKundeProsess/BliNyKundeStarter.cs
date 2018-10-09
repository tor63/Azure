using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class BliNyKundeStarter
    {
        [FunctionName("BliNyKundeStarter")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            TraceWriter log)
        {
            log.Info(" ");
            log.Info("BliNyKundeStarter - HTTP trigger function processed a request.");

            // parse query parameter
            var companyName = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "firmanavn", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            companyName = companyName ?? data?.video;

            if (companyName == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest,
                    "Please pass the 'firmanavn' in the query string or in the request body");
            }

            log.Info($"About to start orchestration for {companyName}");

            var orchestrationId = await starter.StartNewAsync("O_BliNyKunde", companyName);

            return starter.CreateCheckStatusResponse(req, orchestrationId);
        }
    }
}
