using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class StartMangeFirma
    {
        [FunctionName("StartMangeFirma")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            TraceWriter log)
        {
            log.Info(" ");
            log.Info("StartMangeFirma - HTTP trigger function processed a request.");


            // parse query parameter
            var antall = req.GetQueryNameValuePairs()
              .FirstOrDefault(q => string.Compare(q.Key, "antall", System.StringComparison.OrdinalIgnoreCase) == 0)
              .Value;


            //if (antall == null)
            //{
            //    // Get request body
            //    dynamic data = await req.Content.ReadAsAsync<object>();
            //    antall = data?.name;
            //}

            //return antall == null
            //    ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
            //    : req.CreateResponse(HttpStatusCode.OK, "Antall " + antall);

            if (antall == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest,
                    "Please pass the 'Antall' in the query string or in the request body");
            }
            log.Info($"About to start orchestration for {antall} firma");


            if (!System.Int32.TryParse(antall, out int j))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest,
                   "Please pass the 'Antall'som et heltall");
            }

            var ids = "";
            for (var i = 0; i < j; i++)
            {
                var orchestrationId = starter.StartNewAsync("O_BliNyKunde", "Auto" + i);
                ids = orchestrationId + ", ";
                await Task.Delay(10000);
            }


            return req.CreateResponse(HttpStatusCode.OK, "Antall nykunde-prosesserstartet: " + antall + ". OrchestrationIds: " + ids);
        }
    }
}
