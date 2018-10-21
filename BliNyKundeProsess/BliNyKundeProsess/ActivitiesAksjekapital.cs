using System;
using System.Configuration;
using System.Threading.Tasks;
using BliNyKundeClassLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class ActivitiesAksjekapital
    {
        [FunctionName("A_GetAksjekapitalRetries")]
        public static int GetAksjekapitalRetries(
            [ActivityTrigger] object input,
            TraceWriter log)
        {
            //Leser fra Config. Lokalkjøring er dette fra filen 'local.settings.json'.
            //I Azure ligger denne konfigurasjonen i FunctionApp.AppSettings
            var retries= Convert.ToInt32(ConfigurationManager.AppSettings["AksjeKapitalSjekkRetries"]);

            log.Info($"Antall AksjeKapitalSjekkRetries: {retries} hentet fra konfigurasjon");
            return retries;
        }

        [FunctionName("A_SendAksjekapitalRequestEmail")]
        public static async Task SendAksjekapitalRequestEmail(
            [ActivityTrigger] AksjekapitalsMelding melding,
            TraceWriter log)
        {
            log.Info(" ");
            if (melding.Meldingsnummer == 0)
            {
                log.Info($"Sender første melding på epost om aksjekapital til kunde: {melding}");
            }
            else
            {
                log.Info($"Sender PURREMELDING på epost om aksjekapital til kunde: {melding}");
            }

            // simulate doing the activity
            await Task.Delay(5000);
        }

        [FunctionName("A_SendMeldingTilKontoService")]
        public static void SendMeldingTilKontoService(
            [ActivityTrigger] InnbetalingsInfo innbetalingsInfo,
            [Table("Innbetalinger", "AzureWebJobsStorage")] out TableStoreItem aksjekap,
            TraceWriter log)
        {
            log.Info(" ");
            log.Info($"Sender melding til kontotjeneste for OrchestrationId: {innbetalingsInfo.OrchestrationId}");

            var code = Guid.NewGuid().ToString("N");
            aksjekap = new TableStoreItem
            {
                PartitionKey = "Aksjekapital",
                RowKey = code,
                OrchestrationId = innbetalingsInfo.OrchestrationId
            };

            log.Info($"Legger info om Innbetaling i Azure Table Storage: {aksjekap}"); //TODO sjekk logg

            //TODO, hent fra konfig.: var host = ConfigurationManager.AppSettings["Host"]; 
            var host = ConfigurationManager.AppSettings["Host"];
            // var host = "http://localhost:7071";
            var innbetalingsUrl = $"{host}/api/SubmitInnbetaling/{code}";

            log.Info($"Aksjekapital innbetalings url: {innbetalingsUrl}"); //TODO sjekk logg
            //Lag en http trigger event fra service...
            //Som en midlertidig løsning legges denne i Postman
        }
    }
}
