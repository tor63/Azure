using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BliNyKundeClassLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class SigneringsServiceHttpTrigger
    {
        [FunctionName("SigneringsServiceHttpTrigger")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "SubmitSignering/{id}")]
            HttpRequestMessage req,
                        [OrchestrationClient] DurableOrchestrationClient client,
            [Table("Signeringer", "Signering", "{id}", Connection = "AzureWebJobsStorage")] Signering signering,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // nb if the signering code doesn't exist, framework just returns a 404 before we get here
            string result = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "result", true) == 0).Value;

            if (result == null)
                return req.CreateResponse(HttpStatusCode.BadRequest, "Trenger et signeringsresultat");


            log.Warning($"Sending Signerings Resultat to {signering.OrchestrationId} of {result}");

            // send the SigneringsResult external event to this orchestration
            await client.RaiseEventAsync(signering.OrchestrationId, "SigneringsResult", result);

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
