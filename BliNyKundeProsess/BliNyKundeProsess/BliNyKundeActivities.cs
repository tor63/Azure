﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BliNyKundeClassLibrary;
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
            log.Info($"Oppretter midlertidig kundenummer for: {firmanavn}");

            // simulate doing the activity
            const string midlertidigkundenummer = "33112233445"; //use GUID instead??? To get a unique number.
            await Task.Delay(5000);

            //Test throwing exception
            if (firmanavn.ToUpper().Contains("ERROR"))
            {
                throw new InvalidOperationException("Ugyldig firmanavn");
            }

            return midlertidigkundenummer;
        }

        [FunctionName("A_OpprettDriftskonto")]
        public static async Task<string> OpprettDriftskonto(
                [ActivityTrigger] string kundenummerTemp,
                TraceWriter log)
        {
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
            log.Info($"Sender Email om aksjekapital til kunde: {kundenummerTemp}");

            // simulate doing the activity
            await Task.Delay(5000);
        }

        //A_GetSignatarer

        [FunctionName("A_GetSignatarer")]
        public static List<Signatar> GetSignatarer(
            [ActivityTrigger] string kundenummer,
            TraceWriter log)
        {
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
            log.Info($"Henter liste av signatarer for kunde:{kundenummer} ");

            return new Signatar { Navn = "Ola", Epostadresse = "ola@gmail.com", Mobilnummer = "+47 33333333" };
        }

        [FunctionName("A_SendSignMessage")]
        public static async Task SendSignMessage(
            [ActivityTrigger] Signatar signatar,
            TraceWriter log)
        {
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
            [Table("Signeringer", "AzureWebJobsStorage")] out Signering signering,
            TraceWriter log)
        {
            log.Info($"Sender melding til signeringsservice om signeringer for OrchestrationId: {signeringsInfo.OrchestrationId}");

            var signeringsCode = Guid.NewGuid().ToString("N");
            signering = new Signering
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

        }


        [FunctionName("A_Cleanup")]
        public static async Task<string> Cleanup(
            [ActivityTrigger] string kundenummerTemp,
            TraceWriter log)
        {
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