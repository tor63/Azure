using System.Threading;
using System.Threading.Tasks;
using BliNyKundeClassLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class OrchestratorsAksjeKapital
    {
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
                sjekkInnbetalingsResultat = await ctx.CallSubOrchestratorAsync<string>("O_SjekkInnbetalingAksjekapital", 3); //TODO 

                if (sjekkInnbetalingsResultat == "BeløpInnbetalt")
                {
                    if (!ctx.IsReplaying)
                        log.Info("Aksjekapital betalt i tide.");
                    break;
                }
            }
            return sjekkInnbetalingsResultat;

        }

        [FunctionName("O_SjekkInnbetalingAksjekapital")]
        public static async Task<string> SjekkInnbetalingAksjekapital(
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
    }
}
