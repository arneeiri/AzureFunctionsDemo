using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Contracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Publishing.Messages;

namespace Publishing
{
    public static class Functions
    {
        [FunctionName("StartPublishCommand")]
        public async static void HandleStartPublishCommand([QueueTrigger("startpublish", Connection = "AzureWebJobsStorage")] StartPublishCommand command, [Queue("publishdirectory")]ICollector<PublishDirectoryCommand> publishDirectoriesCommands, TraceWriter log)
        {
            log.Info("Starting publish process");

            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
            var cloudFileClient = storageAccount.CreateCloudFileClient();
            var share = cloudFileClient.GetShareReference("content");

            await share.CreateIfNotExistsAsync();
            var rootDirectoryReference = share.GetRootDirectoryReference();

            var directories = new List<CloudFileDirectory>();
            var fileContinuationToken = new FileContinuationToken();
            do
            {
                var fileResultSegment = await rootDirectoryReference.ListFilesAndDirectoriesSegmentedAsync(fileContinuationToken);
                var cloudFileDirectories = fileResultSegment.Results.Where(f => f is CloudFileDirectory).Cast<CloudFileDirectory>();
                directories.AddRange(cloudFileDirectories);

                fileContinuationToken = fileResultSegment.ContinuationToken;
            } while (fileContinuationToken != null);

            foreach (var listFileItem in directories)
            {
                publishDirectoriesCommands.Add(new PublishDirectoryCommand {DirectoryName = listFileItem.Name});
            }

            log.Info($"Sent messages to publish {directories.Count} directories");
            var containerReference = storageAccount.CreateCloudBlobClient().GetContainerReference("web");
            var blobContainerPermissions = await containerReference.GetPermissionsAsync();
            blobContainerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
            await containerReference.SetPermissionsAsync(blobContainerPermissions);
        }

        [FunctionName("PublishDirectoryCommand")]
        public async static void HandlePublishDirectoryCommand([QueueTrigger("publishdirectory", Connection = "AzureWebJobsStorage")] PublishDirectoryCommand command, [Queue("publishimage")]ICollector<PublishImageCommand> publishImageCommands, [Queue("publishpage")]ICollector<PublishPageCommand> publishPageCommands, TraceWriter log)
        {
            log.Info("Starting publish process");

            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);
            var cloudFileClient = storageAccount.CreateCloudFileClient();
            var share = cloudFileClient.GetShareReference("content");

            var rootDirectoryReference = share.GetRootDirectoryReference();
            var cloudFileDirectory = rootDirectoryReference.GetDirectoryReference(command.DirectoryName);

            var files = new List<CloudFile>();
            var fileContinuationToken = new FileContinuationToken();
            do
            {
                var fileResultSegment = await cloudFileDirectory.ListFilesAndDirectoriesSegmentedAsync(fileContinuationToken);
                var cloudFileDirectories = fileResultSegment.Results.Where(f => f is CloudFile).Cast<CloudFile>();
                files.AddRange(cloudFileDirectories);

                fileContinuationToken = fileResultSegment.ContinuationToken;
            } while (fileContinuationToken != null);

            var images = files.Where(f => f.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (files.Any(f => f.Name == "content.txt"))
            {
                publishPageCommands.Add(new PublishPageCommand {DirectoryName = command.DirectoryName, ImageCount = images.Length});
            }

            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                publishImageCommands.Add(new PublishImageCommand {DirectoryName = command.DirectoryName, ImageName = image.Name, ImageNumber = i});
            }
        }
    }
}
