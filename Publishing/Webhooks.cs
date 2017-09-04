using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Publishing.Messages;

namespace Publishing
{
    public static class Webhooks
    {
        [FunctionName("StartPublish")]
        public static HttpResponseMessage HandleStartPublish([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "startPublish")]HttpRequestMessage req, [Queue("startpublish", Connection = "AzureWebJobsStorage")] out StartPublishCommand command, TraceWriter log)
        {
            log.Info("Start publish requested from Webhook");
            command = new StartPublishCommand();
            return req.CreateResponse(HttpStatusCode.OK, new { message = "Publish started!" }, new JsonMediaTypeFormatter());
        }

        [FunctionName("MapFileShare")]
        public static HttpResponseMessage HandleMapFileShare([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "map/{driveLetter}")]HttpRequestMessage req, string driveLetter, TraceWriter log)
        {
            log.Info($"Mapping drive {driveLetter}");

            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);

            var script = $@"net use {driveLetter}: \\{storageAccount.Credentials.AccountName}.file.core.windows.net\content /u:AZURE\{storageAccount.Credentials.AccountName} {ConfigurationManager.AppSettings["AccountKey"]}";

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(script)
            };
            
            httpResponseMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = $"map {driveLetter}.bat" };
            httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return httpResponseMessage;
        }
    }
}