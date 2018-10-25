using System;
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
            var saksResultat = "Unknown";
            var aksjekapitalResultat = "Unknown";
            var signeringsResultat = "Unknown";

            try
            {
                //Chaining Functions
                //Init
                if (!ctx.IsReplaying)
                    log.Info("A_InitNyKunde aktivitet kalles for firma:" + firmanavn);
                kundenummerTemp = await
                    ctx.CallActivityAsync<string>("A_InitNyKunde", firmanavn);

                //Opprett driftskonto
                kontonummer = await
                    ctx.CallActivityAsync<string>("A_OpprettDriftskonto", kundenummerTemp);

                if (false)
                {
                    //Innbetaling av aksjekapital
                    var aksjeKapitalSjekkRetries = await ctx.CallActivityAsync<int>("A_GetAksjekapitalRetries", null);
                    aksjekapitalResultat = await ctx.CallSubOrchestratorAsync<string>("O_SendOgSjekkAksjekapitalWithRetry", aksjeKapitalSjekkRetries);

                    if (aksjekapitalResultat != "BeløpInnbetalt")
                    {
                        if (!ctx.IsReplaying)
                            log.Info("Innbetaling ikke utført i tide, sak avsluttes");
                        saksResultat = await ctx.CallActivityAsync<string>("A_RyddOgAvsluttSak", kundenummerTemp);
                        return new
                        {
                            KundenummerTemp = kundenummerTemp,
                            Kontonummer = kontonummer,
                            AksjekapitalResultat = aksjekapitalResultat,
                            SaksResultat = saksResultat
                        };
                    }
                }

                if (false)
                {    
                    //Signering
                    if (!ctx.IsReplaying)
                        log.Info("O_SendOgSjekkSigneringWithRetry kalles");

                    var signeringsRetries = await ctx.CallActivityAsync<int>("A_GetSigneringsRetries", null);
                    signeringsResultat = await ctx.CallSubOrchestratorAsync<string>("O_SendOgSjekkSigneringWithRetry", signeringsRetries);

                    if (signeringsResultat != "AlleHarSignert")
                    {
                        if (!ctx.IsReplaying)
                            log.Info("Signering ikke utført i tide, sak avsluttes");
                        saksResultat = await ctx.CallActivityAsync<string>("A_RyddOgAvsluttSak", kundenummerTemp);

                        //Stopp videre behandling
                        return new
                        {
                            KundenummerTemp = kundenummerTemp,
                            Kontonummer = kontonummer,
                            AksjekapitalResultat = aksjekapitalResultat,
                            SigneringsResultat = signeringsResultat,
                            SaksResultat = saksResultat
                        };
                    }

                    if (!ctx.IsReplaying)
                        log.Info("Kunde opprettet i banken!! Velkommen som kunde.");
                    saksResultat = "Kundeforhold opprettet.";

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
                }

            }
            catch (Exception e)
            {
                if (!ctx.IsReplaying)
                    log.Error($"Caught an error from an activity: {e.Message}");

                await
                    ctx.CallActivityAsync<string>("A_RyddOgAvsluttSak", kundenummerTemp);

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
                SigneringsResultat = signeringsResultat,
                SaksResultat = saksResultat
            };
        }
    }
}
