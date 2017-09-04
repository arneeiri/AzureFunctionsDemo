using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Contracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Publishing
{
    public static class Functions
    {
        [FunctionName("PublishPageCommand")]
        public static void HandlePublishPageCommand([QueueTrigger("publishpage", Connection = "AzureWebJobsStorage")] PublishPageCommand command, [Blob("web/{DirectoryName}/index.html", FileAccess.ReadWrite, Connection = "AzureWebJobsStorage")] CloudBlockBlob blob, TraceWriter log)
        {
            log.Info($"Publishing page {command.DirectoryName}");

            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
            var cloudFileClient = storageAccount.CreateCloudFileClient();
            var share = cloudFileClient.GetShareReference("content");

            var rootDirectoryReference = share.GetRootDirectoryReference();
            var cloudFileDirectory = rootDirectoryReference.GetDirectoryReference(command.DirectoryName);

            var fileReference = cloudFileDirectory.GetFileReference("content.txt");
            var text = fileReference.DownloadText();

            var html = Template(text, command.ImageCount);
            blob.UploadText(html);
            blob.Properties.ContentType = "text/html";
            blob.SetProperties();
        }

        private static string Template(string text, int imageCount)
        {
            var imageTags = new List<string>();
            for (int i = 0; i < imageCount; i++)
            {
                imageTags.Add($"<img src='{i}.jpg' />");
                
            }
            var html = $@"<html><body><p>{text}</p><div>{string.Join("", imageTags)}</div></body></html>";
            return html;
        }

       
    }
}
