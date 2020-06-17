using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AudioConvertorFunction
{
    public static class AudioConvertorFunction
    {
        [FunctionName("AudioConvertorFunction")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] Stream req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            log.LogInformation("C# HTTP trigger function processed a request.");
            var temp = Path.GetTempFileName() + ".webm";
            var tempOut = Path.GetTempFileName() + ".wav";
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            StringBuilder builder = new StringBuilder();
            string _line = string.Empty;
            int _index = 0;
            JsonDocument document = JsonDocument.Parse(req);
            
            JsonElement element = document.RootElement;
            string strJson = String.Empty;


            if (element.ValueKind == JsonValueKind.Array)
            {
                JsonElement elementOne = element[0];
                elementOne  = elementOne.GetProperty("Url");
                strJson = elementOne.GetRawText();
            }
            else if(element.ValueKind == JsonValueKind.String)
            {
                strJson = element.GetRawText();
            }
             
            strJson = strJson.Replace("data:audio/webm;base64,", "");
            strJson = strJson.Replace("\"", "");

            var audioBuffer = Convert.FromBase64String(strJson);

            using (var ms = new MemoryStream(audioBuffer))
            {

                File.WriteAllBytes(temp, ms.ToArray());
            }
            var bs = File.ReadAllBytes(temp);
            log.LogInformation($"Renc Length: { bs.Length}");
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = "ffmpeg.exe";
                psi.Arguments = $"-i \"{ temp}\" \"{ tempOut}\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                var process = Process.Start(psi);
                process.WaitForExit((int)TimeSpan.FromSeconds(60).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
            }
            var bytes = File.ReadAllBytes(tempOut);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(new MemoryStream(bytes));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            Directory.Delete(tempPath, true);

            return response != null
                ? (ActionResult)new OkObjectResult($"Hello, {response}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
