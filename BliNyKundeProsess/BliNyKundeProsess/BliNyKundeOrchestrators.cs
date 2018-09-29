using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class BliNyKundeOrchestrators
    {
        [FunctionName("O_BliNyKunde")]
        public static async Task<object> BliNyKunde(
            [OrchestrationTrigger] DurableOrchestrationContext ctx, //Dette forteller Azure at dette er en Orchestrator function
            TraceWriter log) //Azure function writer
        {
            var firmanavn = ctx.GetInput<string>();

            if (!ctx.IsReplaying)
                log.Info("About to call INIT nykunde activity");

            string kundenummerTemp;
            string kontonummer;

            try
            {
                kundenummerTemp = await
                    ctx.CallActivityAsync<string>("A_InitNyKunde", firmanavn);

                if (!ctx.IsReplaying)
                    log.Info("About to call extract thumbnail");

                kontonummer = await
                    ctx.CallActivityAsync<string>("A_OpprettDriftskonto", kundenummerTemp);
            }
            catch (Exception e)
            {
                if (!ctx.IsReplaying)
                    log.Info($"Caught an error from an activity: {e.Message}");

                //await
                //    ctx.CallActivityAsync<string>("A_Cleanup",
                //        new[] { kundenummerTemp, kontonummer});

                return new
                {
                    Error = "Failed to process uploaded video",
                    Message = e.Message
                };
            }

            return new
            {
                KundenummerTemp = kundenummerTemp,
                Kontonummer = kontonummer
            };
        }
    }
}
