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

        //[FunctionName("O_TestUserEvent")]
        //public static async Task<string> TestUserEvent(
        //        [OrchestrationTrigger] DurableOrchestrationContext ctx,
        //        TraceWriter log)
        //{
        //    string sjekkSigneringsResult = "Unknown";
        //    string sendSigneringsResults = "Unknown";

        //    //Merk Exceptions bobbler opp til hovedrok. funksjonen.
        //    var kundenummer = ctx.GetInput<string>();
        //    var signatar1 = await ctx.CallActivityAsync<Signatar>("A_GetSignatar", kundenummer);
        //    var signarliste = new List<Signatar>();
        //    signarliste.Add(signatar1);

        //    await ctx.CallActivityAsync<string>("A_SendSignMessageToService", new SigneringsInfo()
        //    {
        //        OrchestrationId = ctx.InstanceId,
        //        signatarer = signarliste
        //    });


        //    using (var cts = new CancellationTokenSource())
        //    {
        //        //TODO: Timeout fra konfig - 2 uker i prod, Kort tid i test
        //        var timeoutAt = ctx.CurrentUtcDateTime.AddHours(2);

        //        //Oppretter 2 oppgaver og sjekker hvilken som er ferdig først
        //        //Forenkler: Signeringsstatus rapporteres fra en seperat tjeneste. Trenger da kun å lytte på en event.
        //        var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);
        //        var sjekkSigneringsTask = ctx.WaitForExternalEvent<string>("SigneringsResult");

        //        //Kan benytte DF REST Api for å sende event.

        //        var winner = await Task.WhenAny(sjekkSigneringsTask, timeoutTask);
        //        if (winner == sjekkSigneringsTask)
        //        {
        //            sjekkSigneringsResult = sjekkSigneringsTask.Result;
        //            cts.Cancel(); // we should cancel the timeout task
        //        }
        //        else
        //        {
        //            sjekkSigneringsResult = "Timed Out";
        //        }
        //    }

        //    if (sjekkSigneringsResult == "AlleHarSignert")
        //    {
        //        if (!ctx.IsReplaying)
        //            log.Info("Signering utført i tide. ");
        //    }
        //    else
        //    {
        //        if (!ctx.IsReplaying)
        //            log.Info("Signering ikke utført i tide. ");
        //    }
        //    return sjekkSigneringsResult;
        //}

        #region Aksjekapital
        [FunctionName("O_SendOgSjekkAksjekapitalWithRetry")]
        public static async Task<string> SendOgSjekkAksjekapitalWithRetry(
                  [OrchestrationTrigger] DurableOrchestrationContext ctx,
                  TraceWriter log)
        {
            //    //Merk Exceptions bobbler opp til hovedork. funksjonen.
            var retries = ctx.GetInput<int>();
            if (!ctx.IsReplaying)
                log.Info($"O_SendOgSjekkAksjekapitalWithRetry aktivitet kalles med antall retries: {retries}");

            var sjekkInnbetalingsResultat = "Unknown";

            for (var retryCount = 0; retryCount < retries; retryCount++)
            {
                if (!ctx.IsReplaying)
                    log.Info("A_SendAksjekapitalRequestEmail aktivitet kalles: " + retryCount + 1 + ". gang");
                await ctx.CallActivityAsync("A_SendAksjekapitalRequestEmail", "123123"); //TODO: Kundeinfo inn her


                //TODO sjekk at sending gikk OK - HVORDAN??
                //Start sjekk om aksjekapital er innbetalt

                // Sjekk innbetaling til konto >= aksjekapitalbeløp
                // Hvis ikke innbetalt innen frist
                //  Sendpurring 1 gang med ny frist.
                //  Dersom denne fristen også utløper: Send melding til kunde og avslutt sak

                if (!ctx.IsReplaying)
                    log.Info("sjekker innbetaling: " + retryCount + 1 + ". gang");
                sjekkInnbetalingsResultat = await ctx.CallSubOrchestratorAsync<string>("O_SjekkInnbetalingAksjekaital", 3); //TODO 

                if (sjekkInnbetalingsResultat == "BeløpInnbetalt")
                {
                    if (!ctx.IsReplaying)
                        log.Info("Aksjekapital betalt i tide.");
                    break;
                }
            }
            return sjekkInnbetalingsResultat;

        }

        [FunctionName("O_SjekkInnbetalingAksjekaital")]
        public static async Task<string> SjekkInnbetalingAksjekaital(
             [OrchestrationTrigger] DurableOrchestrationContext ctx,
             TraceWriter log)
        {
            var n = ctx.GetInput<int>();
            string sjekkSigneringsResult = "Unknown";

            await ctx.CallActivityAsync<string>("A_SendMeldingTilKontoService", new InnbetalingsInfo()
            {
                OrchestrationId = ctx.InstanceId,
                Amount = 30000
            });


            using (var cts = new CancellationTokenSource())
            {
                //TODO: Timeout fra konfig - 2 uker i prod, Kort tid i test
                var timeoutAt = ctx.CurrentUtcDateTime.AddHours(2);

                //Oppretter 2 oppgaver og sjekker hvilken som er ferdig først
                //Forenkler: Signeringsstatus rapporteres fra en seperat tjeneste. Trenger da kun å lytte på en event.
                var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);
                var sjekkSigneringsTask = ctx.WaitForExternalEvent<string>("InnbetalingsResult");

                var winner = await Task.WhenAny(sjekkSigneringsTask, timeoutTask);
                if (winner == sjekkSigneringsTask)
                {
                    sjekkSigneringsResult = sjekkSigneringsTask.Result;
                    cts.Cancel(); // we should cancel the timeout task
                }
                else
                {
                    sjekkSigneringsResult = "Timed Out";
                }
            }

            if (sjekkSigneringsResult == "BeløpInnbetalt")
            {
                if (!ctx.IsReplaying)
                    log.Info("Aksjekapital er innbetalt. ");
            }
            else
            {
                if (!ctx.IsReplaying)
                    log.Info("Aksjekapital er IKKE innbetalt i tide.");
            }
            return sjekkSigneringsResult;
        }

        //[FunctionName("O_SjekkSignering")]
        //public static async Task<string> SjekkSignering(
        //         [OrchestrationTrigger] DurableOrchestrationContext ctx,
        //         TraceWriter log)
        //{
        //    //Denne retry-mekanismen fungerer ikke helt, hva bør en gjøre? Kanskje kaste en exception som en fanger i retry på utsiden?

        //    string sjekkSigneringsResult = "Unknown";
        //    using (var cts = new CancellationTokenSource())
        //    {
        //        var timeoutAt = ctx.CurrentUtcDateTime.AddSeconds(20);

        //        //Oppretter 2 oppgaver og sjekker hvilken som er ferdig først
        //        var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);


        //        for (int retryCount = 0; retryCount <= 1; retryCount++)
        //        {
        //            var sjekkSigneringsTask = ctx.WaitForExternalEvent<string>("ApprovalResult");

        //            var winner = await Task.WhenAny(sjekkSigneringsTask, timeoutTask);
        //            if (winner == sjekkSigneringsTask)
        //            {
        //                sjekkSigneringsResult = sjekkSigneringsTask.Result;
        //                break;
        //                //cts.Cancel(); // we should cancel the timeout task
        //            }
        //            else
        //            {
        //                sjekkSigneringsResult = "Timed Out";
        //                break;
        //            }
        //        }

        //        if (!timeoutTask.IsCompleted)
        //        {
        //            cts.Cancel(); // we should cancel the timeout task
        //        }

        //        return sjekkSigneringsResult;
        //    }
        //}
        #endregion Aksjekapital

        #region Signering
        [FunctionName("O_SendOgSjekkSigneringWithRetry")]
        public static async Task<string> SendOgSjekkSigneringWithRetry(
                  [OrchestrationTrigger] DurableOrchestrationContext ctx,
                  TraceWriter log)
        {
            //    //Merk Exceptions bobbler opp til hovedrok. funksjonen.
            var retries = ctx.GetInput<int>();
            string sjekkSigneringsResult = "Unknown";

            for (int retryCount = 0; retryCount < retries; retryCount++)
            {
                if (!ctx.IsReplaying)
                    log.Info("sender signering: " + retryCount + 1 + ". gang");
                var signatarer = await ctx.CallSubOrchestratorAsync<List<Signatar>>("O_SendSignering", null);

                //TODO sjekk at sending gikk OK - HVORDAN??

                if (!ctx.IsReplaying)
                    log.Info("sjekker signering: " + retryCount + 1 + ". gang");
                sjekkSigneringsResult = await ctx.CallSubOrchestratorAsync<string>("O_SjekkSignering", signatarer);
                if (sjekkSigneringsResult == "AlleHarSignert")
                {
                    if (!ctx.IsReplaying)
                        log.Info("Signering utført i tide. ");
                    break;
                }
            }
            return sjekkSigneringsResult;

        }

        [FunctionName("O_SendSignering")]
        public static async Task<List<Signatar>> SendSignering(
            [OrchestrationTrigger] DurableOrchestrationContext ctx,
            TraceWriter log)
        {
            //Merk Exceptions bobbler opp til hovedrok. funksjonen.
            var kundenummer = ctx.GetInput<string>();
            var signatarer = await ctx.CallActivityAsync<List<Signatar>>("A_GetSignatarer", kundenummer);
            var signeringTasks = new List<Task<string>>();

            foreach (var s in signatarer)
            {
                var task = ctx.CallActivityAsync<string>("A_SendSignMessage", s);
                signeringTasks.Add(task);
            }

            var sendSigneringsResults = await Task.WhenAll(signeringTasks);
            return signatarer;
        }



        //[FunctionName("O_SjekkSigneringWithRetry")]
        //public static async Task<string> SjekkSigneringWithRetry(
        //           [OrchestrationTrigger] DurableOrchestrationContext ctx,
        //           TraceWriter log)
        //{
        //    //    //Merk Exceptions bobbler opp til hovedrok. funksjonen.
        //    var retries = ctx.GetInput<int>();
        //    string sjekkSigneringsResult = "Unknown";

        //    for (int retryCount = 0; retryCount < retries; retryCount++)
        //    {
        //        if (!ctx.IsReplaying)
        //            log.Info("sjekker signering:" + retryCount + 1 + ". gang");
        //        sjekkSigneringsResult = await ctx.CallSubOrchestratorAsync<string>("O_SjekkSignering", null);
        //        if (sjekkSigneringsResult == "Approved")
        //        {
        //            break;
        //        }
        //    }
        //    return sjekkSigneringsResult;

        //}

        [FunctionName("O_SjekkSignering")]
        public static async Task<string> SjekkSignering(
             [OrchestrationTrigger] DurableOrchestrationContext ctx,
             TraceWriter log)
        {
            var signarliste = ctx.GetInput<List<Signatar>>();
            string sjekkSigneringsResult = "Unknown";

            await ctx.CallActivityAsync<string>("A_SendSignMessageToService", new SigneringsInfo()
            {
                OrchestrationId = ctx.InstanceId,
                signatarer = signarliste
            });

            using (var cts = new CancellationTokenSource())
            {
                //TODO: Timeout fra konfig - 2 uker i prod, Kort tid i test
                var timeoutAt = ctx.CurrentUtcDateTime.AddHours(2);

                //Oppretter 2 oppgaver og sjekker hvilken som er ferdig først
                //Forenkler: Signeringsstatus rapporteres fra en seperat tjeneste. Trenger da kun å lytte på en event.
                var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);

                var sjekkSigneringsTask = ctx.WaitForExternalEvent<string>("SigneringsResult");

                //Kan benytte DF REST Api for å sende event.

                var winner = await Task.WhenAny(sjekkSigneringsTask, timeoutTask);
                if (winner == sjekkSigneringsTask)
                {
                    sjekkSigneringsResult = sjekkSigneringsTask.Result;
                    cts.Cancel(); // we should cancel the timeout task
                }
                else
                {
                    sjekkSigneringsResult = "Timed Out";
                }
            }

            if (sjekkSigneringsResult == "AlleHarSignert")
            {
                if (!ctx.IsReplaying)
                    log.Info("Signering utført i tide. ");
            }
            else
            {
                if (!ctx.IsReplaying)
                    log.Info("Signering ikke utført i tide. ");

                //await ctx.CallActivityAsync("A_Cleanup", kundenummerTemp);
            }
            return sjekkSigneringsResult;
        }

        //[FunctionName("O_SjekkSignering")]
        //public static async Task<string> SjekkSignering(
        //         [OrchestrationTrigger] DurableOrchestrationContext ctx,
        //         TraceWriter log)
        //{
        //    //Denne retry-mekanismen fungerer ikke helt, hva bør en gjøre? Kanskje kaste en exception som en fanger i retry på utsiden?

        //    string sjekkSigneringsResult = "Unknown";
        //    using (var cts = new CancellationTokenSource())
        //    {
        //        var timeoutAt = ctx.CurrentUtcDateTime.AddSeconds(20);

        //        //Oppretter 2 oppgaver og sjekker hvilken som er ferdig først
        //        var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);


        //        for (int retryCount = 0; retryCount <= 1; retryCount++)
        //        {
        //            var sjekkSigneringsTask = ctx.WaitForExternalEvent<string>("ApprovalResult");

        //            var winner = await Task.WhenAny(sjekkSigneringsTask, timeoutTask);
        //            if (winner == sjekkSigneringsTask)
        //            {
        //                sjekkSigneringsResult = sjekkSigneringsTask.Result;
        //                break;
        //                //cts.Cancel(); // we should cancel the timeout task
        //            }
        //            else
        //            {
        //                sjekkSigneringsResult = "Timed Out";
        //                break;
        //            }
        //        }

        //        if (!timeoutTask.IsCompleted)
        //        {
        //            cts.Cancel(); // we should cancel the timeout task
        //        }

        //        return sjekkSigneringsResult;
        //    }
        //}
        #endregion Signering
    }
}
