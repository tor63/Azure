using System;
using System.Threading.Tasks;
using DataLayer;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace BliNyKundeProsess
{
    public static class ActivitiesBliNyKunde
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
            IDbSakService db = new DbSakService();
            db.CreateSak(dbsak);

            //Test throwing exception
            if (firmanavn.ToUpper().Contains("ERROR"))
            {
                log.Warning($"Ugyldig firmanavn - inneholder 'ERROR'");
                throw new InvalidOperationException("Ugyldig firmanavn");
            }

            return dbsak.Kundenummer;
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
