using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            log.Info($"Transcoding {firmanavn}");

            // simulate doing the activity
            const string midlertidigkundenummer = "33112233445"; //use GUID instead??? To get a unique number.
            await Task.Delay(5000);

            return midlertidigkundenummer;
        }

        [FunctionName("A_OpprettDriftskonto")]
        public static async Task<string> OpprettDriftskonto(
                [ActivityTrigger] string kundenummerTemp, 
                TraceWriter log)
        {
            log.Info($"Transcoding {kundenummerTemp}");

            // simulate doing the activity
            const string driftskontonummer = "1111.22.33333";
            await Task.Delay(5000);

            return driftskontonummer;
        }
    }
}
