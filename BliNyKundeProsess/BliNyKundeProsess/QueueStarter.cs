using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class QueueStarter
    {
        [FunctionName("QueueStarter")]
        public static void Run(
            [QueueTrigger("myqueue-items", Connection = "AzureWebJobsStorage")]string myQueueItem,
            [OrchestrationClient] DurableOrchestrationClient starter,
            TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");

            var orchestrationId = starter.StartNewAsync("O_BliNyKunde", "Olasinn0001");
        }
    }
}
