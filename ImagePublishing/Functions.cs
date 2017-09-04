using System.Configuration;
using System.Drawing;
using System.IO;
using Contracts;
using ImageProcessor;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;

namespace ImagePublishing
{
    public static class Functions
    {
        [FunctionName("PublishImageCommand")]
        public static void HandlePublishImageCommand([QueueTrigger("publishimage", Connection = "AzureWebJobsStorage")] PublishImageCommand command, [Blob("web/{DirectoryName}/{ImageNumber}.jpg", Connection = "AzureWebJobsStorage")] Stream blob, TraceWriter log)
        {
            log.Info($"Publishing page {command.DirectoryName}");

            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
            var cloudFileClient = storageAccount.CreateCloudFileClient();
            var share = cloudFileClient.GetShareReference("content");

            var rootDirectoryReference = share.GetRootDirectoryReference();
            var cloudFileDirectory = rootDirectoryReference.GetDirectoryReference(command.DirectoryName);

            var fileReference = cloudFileDirectory.GetFileReference(command.ImageName);
            using (var stream = new MemoryStream())
            {
                fileReference.DownloadToStream(stream);
                stream.Position = 0;
                using (var imageFactory = new ImageFactory())
                {
                    imageFactory.
                        Load(stream).
                        Hue(180).
                        Resize(new Size(300, 0)).
                        Save(blob);
                }
            }
        }
    }
}