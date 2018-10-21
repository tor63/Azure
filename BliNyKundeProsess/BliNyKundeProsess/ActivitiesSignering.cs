using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using BliNyKundeClassLibrary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class ActivitiesSignering
    {
        [FunctionName("A_GetSigneringsRetries")]
        public static int GetSigneringsRetries(
            [ActivityTrigger] object input,
            TraceWriter log)
        {
            //Leser fra Config. Lokalkjøring er dette fra filen 'local.settings.json'.
            //I Azure ligger denne konfigurasjonen i FunctionApp.AppSettings
            var retries = Convert.ToInt32(ConfigurationManager.AppSettings["SigneringsRetries"]);

            log.Info($"Antall SigneringsRetries: {retries} hentet fra konfigurasjon");
            return retries;
        }

        [FunctionName("A_GetSignatarer")]
        public static List<Signatar> GetSignatarer(
            [ActivityTrigger] string kundenummer,
            TraceWriter log)
        {
            log.Info(" ");
            log.Info($"Henter liste av signatarer for kunde:{kundenummer} ");

            return new List<Signatar>
            {
                new Signatar {Navn = "Per", Epostadresse = "per@gmail.com", Mobilnummer = "+47 11111111"},
                new Signatar {Navn = "Kari", Epostadresse = "kari@gmail.com", Mobilnummer = "+47 22222222"}
            };
        }

        [FunctionName("A_GetSignatar")]
        public static Signatar GetSignatar(
            [ActivityTrigger] string kundenummer,
            TraceWriter log)
        {
            log.Info(" ");
            log.Info($"Henter liste av signatarer for kunde:{kundenummer} ");

            return new Signatar { Navn = "Ola", Epostadresse = "ola@gmail.com", Mobilnummer = "+47 33333333" };
        }

        [FunctionName("A_SendSignMessage")]
        public static async Task SendSignMessage(
            [ActivityTrigger] Signatar signatar,
            TraceWriter log)
        {
            log.Info(" ");
            log.Info($"Sender Email om signaturtil kunde: {signatar.Navn}");

            //Test throwing exception
            if (signatar.Navn.ToUpper().Contains("ERROR"))
            {
                throw new InvalidOperationException("Ugyldig navn");
            }
            // simulate doing the activity
            await Task.Delay(5000);
        }

        [FunctionName("A_SendSignMessageToService")]
        public static void SendSignMessageToService(
            [ActivityTrigger] SigneringsInfo signeringsInfo,
            [Table("Signeringer", "AzureWebJobsStorage")] out TableStoreItem signering,
            TraceWriter log)
        {
            log.Info(" ");
            log.Info($"Sender melding til signeringsservice om signeringer for OrchestrationId: {signeringsInfo.OrchestrationId}");

            var signeringsCode = Guid.NewGuid().ToString("N");
            signering = new TableStoreItem
            {
                PartitionKey = "Signering",
                RowKey = signeringsCode,
                OrchestrationId = signeringsInfo.OrchestrationId
            };

            log.Info($"Legger info om sigernering i Azure Table Storage: {signering}"); //TODO sjekk logg

            //TODO, hent fra konfig.: var host = ConfigurationManager.AppSettings["Host"]; 
            var host = "http://localhost:7071";
            var signeringsurl = $"{host}/api/SubmitSignering/{signeringsCode}";
            log.Info($"Signerings url: {signeringsurl}"); //TODO sjekk logg

            //Lag en http trigger event fra service...
            //Som en midlertidig løsning legges denne i Postman
        }
    }
}
