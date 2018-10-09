using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class OrchestratorsBliNyKunde
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
            string aksjekapitalResultat = "Unknown";
            string signeringsResultat = "Unknown";


            try
            {
                if (true)
                {
                    //Chaining Functions
                    //Init
                    if (!ctx.IsReplaying)
                        log.Info("A_InitNyKunde aktivitet kalles");
                    kundenummerTemp = await
                        ctx.CallActivityAsync<string>("A_InitNyKunde", firmanavn);

                    //Opprett driftskonto
                    kontonummer = await
                        ctx.CallActivityAsync<string>("A_OpprettDriftskonto", kundenummerTemp);

                    //Innbetaling av aksjekapital
                    var retries = await ctx.CallActivityAsync<int>("A_GetAksjekapitalRetries", null);
                    aksjekapitalResultat = await ctx.CallSubOrchestratorAsync<string>("O_SendOgSjekkAksjekapitalWithRetry", retries);

                    if (aksjekapitalResultat == "BeløpInnbetalt")
                    {
                        //Fortsett..
                    }
                    else
                    {
                        //TODO Stopp videre behandling
                        if (!ctx.IsReplaying)
                            log.Info("Innbetaling ikke utført i tide");
                    }
                }

                if (false)
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        var timeoutAt = ctx.CurrentUtcDateTime.AddSeconds(30);

                        //Oppretter 2 oppgaver og sjekker hvilken som er ferdig først
                        var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);
                        var aksjekapitalApprovalTask = ctx.WaitForExternalEvent<string>("ApprovalResult");

                        var winner = await Task.WhenAny(aksjekapitalApprovalTask, timeoutTask);
                        if (winner == aksjekapitalApprovalTask)
                        {
                            aksjekapitalResultat = aksjekapitalApprovalTask.Result;
                            cts.Cancel(); // we should cancel the timeout task
                        }
                        else
                        {
                            aksjekapitalResultat = "Timed Out";
                        }
                    }

                    if (aksjekapitalResultat == "Approved")
                    {
                        ;
                        //fortsett
                        //await ctx.CallActivityAsync("A_PublishVideo", withIntroLocation);
                    }
                    else
                    {
                        if (!ctx.IsReplaying)
                            log.Info("Aksjekapital ikke innbetalt i tide. Firma ikke opprettet som kunde i banken og sak avsluttes.");
                        await ctx.CallActivityAsync("A_Cleanup", kundenummerTemp);
                    }
                }

                if (false)
                {
                    //Signering
                    if (!ctx.IsReplaying)
                        log.Info("O_SendSignering kalles");
                    //var signeringResults =
                    //    await ctx.CallSubOrchestratorAsync<string[]>("O_Signering", "00986333111");

                    //Retry upto 2 times with 35 seconds delay between each attempt
                    var signeringResults =
                                        await ctx.CallSubOrchestratorWithRetryAsync<string[]>("O_SendSignering", new RetryOptions(TimeSpan.FromSeconds(35), 2),
                                         "00986333111");

                    //Only retry invalid operation Exception
                    //var signeringResults =
                    //    await ctx.CallSubOrchestratorWithRetryAsync<string[]>("O_Signering", new RetryOptions(TimeSpan.FromSeconds(35), 2)
                    //    { Handle = ex => ex is InvalidOperationException }, "00986333111");

                }

                if (false)
                {
                    //Signering
                    if (!ctx.IsReplaying)
                        log.Info("O_SjekkSigneringWithRetry kalles");

                    // var b = await ctx.CallSubOrchestratorAsync<bool>("O_SjekkSignering");
                    //sjekkSigneringsResult = await ctx.CallSubOrchestratorAsync<string>("O_SjekkSignering", null);

                    //TODO: Les retries fra Config!!
                    signeringsResultat = await ctx.CallSubOrchestratorAsync<string>("O_SjekkSigneringWithRetry", 2);

                    //if(sjekkSigneringsResult == "Timed Out")
                    //{
                    //    sjekkSigneringsResult = await ctx.CallSubOrchestratorAsync<string>("O_SjekkSignering", null);

                    //    if (sjekkSigneringsResult == "Timed Out")
                    //    {
                    //        ;
                    //        //Cancel task
                    //    }
                    //}
                }

                if (false)
                {
                    //Signering
                    if (!ctx.IsReplaying)
                        log.Info("O_SendOgSjekkSigneringWithRetry kalles");

                    // var b = await ctx.CallSubOrchestratorAsync<bool>("O_SjekkSignering");
                    //sjekkSigneringsResult = await ctx.CallSubOrchestratorAsync<string>("O_SjekkSignering", null);

                    //TODO: Les retries fra Config!!
                    signeringsResultat = await ctx.CallSubOrchestratorAsync<string>("O_SendOgSjekkSigneringWithRetry", 2);

                    //if(sjekkSigneringsResult == "Timed Out")
                    //{
                    //    sjekkSigneringsResult = await ctx.CallSubOrchestratorAsync<string>("O_SjekkSignering", null);

                    //    if (sjekkSigneringsResult == "Timed Out")
                    //    {
                    //        ;
                    //        //Cancel task
                    //    }
                    //}
                }

                if (false)
                {
                    signeringsResultat = await ctx.CallSubOrchestratorAsync<string>("O_TestUserEvent", 00945333222);
                }

            }
            catch (Exception e)
            {
                if (!ctx.IsReplaying)
                    log.Error($"Caught an error from an activity: {e.Message}");

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
                Kontonummer = kontonummer,
                AksjekapitalResultat = aksjekapitalResultat,
                SigneringsResultat = signeringsResultat
            };
        }
    }
}
