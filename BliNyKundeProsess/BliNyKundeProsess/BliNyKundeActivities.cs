using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using BliNyKundeClassLibrary;
using DataLayer;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class BliNyKundeActivities
    {
        [FunctionName("A_InitNyKunde")]
        public static async Task<string> InitKunde(
            [ActivityTrigger] string firmanavn, //Defines the function is an activity function, Can pass class
            TraceWriter log)
        //Could add bindings as usual Azure Functions
        {
            log.Info(" ");

            //Opprett sak i database
            var dbsak = new DbSak
            {
                Kundenummer = Guid.NewGuid().ToString("N"),
                Kundenavn = firmanavn
            };
            log.Info($"Opprettet midlertidig kundenummer: {dbsak.Kundenummer} for: {firmanavn}");

            log.Info($"Lagrer sak i AFS databasen.");
            DbSakService.CreateSak(dbsak);

            //Test throwing exception
            if (firmanavn.ToUpper().Contains("ERROR"))
            {
                log.Warning($"Ugyldig firmanavn - inneholder 'ERROR'");
                throw new InvalidOperationException("Ugyldig firmanavn");
            }

            return dbsak.Kundenummer;
        }

        [FunctionName("A_GetAksjekapitalRetries")]
        public static int GetAksjekapitalRetries(
            [ActivityTrigger] object input,
            TraceWriter log)
        {
            //Leser fra Config. Lokalkjøring er dette fra filen 'local.settings.json'.
            //I Azure ligger denne konfigurasjonen i FunctionApp.AppSettings
            var retries= Convert.ToInt32(ConfigurationManager.AppSettings["AksjeKapitalSjekkRetries"]);

            log.Info($"Antall retries: {retries} hentet fra konfigurasjon");
            return retries;
        }

        [FunctionName("A_OpprettDriftskonto")]
        public static async Task<string> OpprettDriftskonto(
                [ActivityTrigger] string kundenummerTemp,
                TraceWriter log)
        {
            log.Info(" ");
            log.Info($"Lager driftskonto for kunde: {kundenummerTemp}");

            // simulate doing the activity
            const string driftskontonummer = "1111.22.33333";
            await Task.Delay(5000);

            return driftskontonummer;
        }


        [FunctionName("A_SendAksjekapitalRequestEmail")]
        public static async Task SendAksjekapitalRequestEmail(
        [ActivityTrigger] string kundenummerTemp,
        TraceWriter log)
        {
            log.Info(" ");
            log.Info($"Sender Email om aksjekapital til kunde: {kundenummerTemp}");

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

        #region Signering
        //A_GetSignatarer
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

            log.Info($"Signerings url: {signering}"); //TODO sjekk logg

            //TODO, hent fra konfig.: var host = ConfigurationManager.AppSettings["Host"]; 
            var host = "http://localhost:7071";
            var signeringsurl = $"{host}/api/SubmitSignering/{signeringsCode}";
            //Lag en http trigger event fra service...
            //Som en midlertidig løsning legges denne i Postman
        }
        #endregion Signering

        [FunctionName("A_Cleanup")]
        public static async Task<string> Cleanup(
            [ActivityTrigger] string kundenummerTemp,
            TraceWriter log)
        {
            log.Info(" ");
            if (kundenummerTemp != null)
            {
                log.Info($"Deleting {kundenummerTemp}");
                // simulate doing the activity
                await Task.Delay(1000);
            }
            return "Deleted Customer successfully";
        }
    }
}
