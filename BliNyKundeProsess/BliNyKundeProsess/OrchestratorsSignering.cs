using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BliNyKundeClassLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class OrchestratorsSignering
    {

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

        [FunctionName("O_SendOgSjekkSigneringWithRetry")]
        public static async Task<string> SendOgSjekkSigneringWithRetry(
                  [OrchestrationTrigger] DurableOrchestrationContext ctx,
                  TraceWriter log)
        {
            //    //Merk Exceptions bobbler opp til hovedrok. funksjonen.
            var retries = ctx.GetInput<int>();
            var sjekkSigneringsResult = "Unknown";

            //TODO spesial sending ved purring
            if (!ctx.IsReplaying)
                log.Info("sender melding om signering.");
            var signatarer = await ctx.CallSubOrchestratorAsync<List<Signatar>>("O_SendSignering", null);

            for (var retryCount = 0; retryCount < retries; retryCount++)
            {
                //Send signering
                if (retryCount > 0)
                {
                    if (!ctx.IsReplaying)
                        log.Info("sender purring om signering: " + retryCount + 1 + ". gang");
                    signatarer = await ctx.CallSubOrchestratorAsync<List<Signatar>>("O_SendSignering", null);
                }

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

            await Task.WhenAll(signeringTasks);
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

                //await ctx.CallActivityAsync("A_RyddOgAvsluttSak", kundenummerTemp);
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
    }
}
