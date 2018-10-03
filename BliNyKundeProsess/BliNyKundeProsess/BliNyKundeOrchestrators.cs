using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BliNyKundeClassLibrary;
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

            string kundenummerTemp = null;
            string kundenummer = null;
            string kontonummer = null;
            string aksjekapitalApprovalResult = "Unknown";

            try
            {
                if (false)
                {
                    //Chaining Functions
                    if (!ctx.IsReplaying)
                        log.Info("About to call A_InitNyKunde activity");

                    kundenummerTemp = await
                        ctx.CallActivityAsync<string>("A_InitNyKunde", firmanavn);

                    if (!ctx.IsReplaying)
                        log.Info("About to call A_OpprettDriftskonto activity");

                    kontonummer = await
                        ctx.CallActivityAsync<string>("A_OpprettDriftskonto", kundenummerTemp);

                    //Send sms og epost
                    if (!ctx.IsReplaying)
                        log.Info("About to call A_SendAksjekapitalRequestEmail activity");
                    await ctx.CallActivityAsync("A_SendAksjekapitalRequestEmail", kundenummerTemp);
                }

                //Start sjekk om aksjekapital er innbetalt


                // Sjekk innbetaling til konto >= aksjekapitalbeløp
                // Hvis ikke innbetalt innen frist
                //  Sendpurring 1 gang med ny frist.
                //  Dersom denne fristen også utløper: Send melding til kunde og avslutt sak


                //using (var cts = new CancellationTokenSource())
                //{
                //    var timeoutAt = ctx.CurrentUtcDateTime.AddSeconds(30);

                //    //Oppretter 2 oppgaver og sjekker hvilken som er ferdig først
                //    var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);
                //    var aksjekapitalApprovalTask = ctx.WaitForExternalEvent<string>("ApprovalResult");

                //    var winner = await Task.WhenAny(aksjekapitalApprovalTask, timeoutTask);
                //    if (winner == aksjekapitalApprovalTask)
                //    {
                //        aksjekapitalApprovalResult = aksjekapitalApprovalTask.Result;
                //        cts.Cancel(); // we should cancel the timeout task
                //    }
                //    else
                //    {
                //        aksjekapitalApprovalResult = "Timed Out";
                //    }
                //}

                //if (aksjekapitalApprovalResult == "Approved")
                //{
                //    ;
                //    //fortsett
                //    //await ctx.CallActivityAsync("A_PublishVideo", withIntroLocation);
                //}
                //else
                //{
                //    if (!ctx.IsReplaying)
                //        log.Info("Aksjekapital ikke innbetalt i tide. Firma ikke opprettet som kunde i banken og sak avsluttes.");
                //    await ctx.CallActivityAsync("A_Cleanup", kundenummerTemp);
                //}

                //Signering
                if (!ctx.IsReplaying)
                    log.Info("O_Signering kalles");
                var signeringResults =
                    await ctx.CallSubOrchestratorAsync<string[]>("O_Signering", "00986333111");

            }
            catch (Exception e)
            {
                if (!ctx.IsReplaying)
                    log.Info($"Caught an error from an activity: {e.Message}");

                await
                    ctx.CallActivityAsync<string>("A_Cleanup", kundenummerTemp);

                return new
                {
                    Error = "Failed to process BliNyKunde",
                    Message = e.Message
                };
            }

            return new
            {
                KundenummerTemp = kundenummerTemp,
                Kontonummer = kontonummer
            };
        }

        [FunctionName("O_Signering")]
        public static async Task<string[]> Signering(
            [OrchestrationTrigger] DurableOrchestrationContext ctx,
            TraceWriter log)
        {
            var kundenummer = ctx.GetInput<string>();
            var signatarer = await ctx.CallActivityAsync<List<Signatar>>("A_GetSignatarer", kundenummer);
            var signeringTasks = new List<Task<string>>();

            foreach (var s in signatarer)
            {
                var task = ctx.CallActivityAsync<string>("A_SendSignMessage", s);
                signeringTasks.Add(task);
            }

            var signeringsResults = await Task.WhenAll(signeringTasks);
            return signeringsResults;
        }
    }
}
