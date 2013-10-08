using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using AzureCrawlerRole.Model;
using System.Diagnostics;
using System.Web;

namespace AzureCrawlerRole.Helpers
{
    public static class AzureHelper
    {
        private static CloudStorageAccount StorageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"));

        private static CloudBlobClient BlobClient = StorageAccount.CreateCloudBlobClient();

        /// <summary>
        /// Create container for app
        /// Generates a blob container with the application name for storing the snaphots
        /// </summary>
        /// <param name="application">The app container</param>
        /// <returns>bool</returns>
        public static bool CreateAppContainerIfNotExists(string application)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(application);

            try
            {
                container.CreateIfNotExists();

                container.SetPermissions(
                    new BlobContainerPermissions
                    {
                        PublicAccess =
                            BlobContainerPublicAccessType.Blob
                    });
            }
            catch (StorageException ex)
            {
                return false;
            }

            Trace.TraceInformation("Container created", application);

            return true;
        }

        /// <summary>
        /// Get the html content for being returned to google bot
        /// </summary>
        /// <param name="application">The target container</param>
        /// <param name="url">The target url for get then html content</param>
        /// <returns>Html static content</returns>
        public static string ReadSnapshot(string application, string url)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(application);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(HttpUtility.UrlEncode(url));

            string html;

            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                html = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            Trace.TraceInformation("Snapshot readed", application, url);

            return html;
        }

        /// <summary>
        /// Upload a blob to azure
        /// </summary>
        /// <param name="application">The target container</param>
        /// <param name="url">The target url</param>
        /// <param name="data">The text to upload</param>
        public static void SaveSnapshot(string application, string url, string text)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(application);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(HttpUtility.UrlEncode(url));

            blockBlob.UploadText(text);

            Trace.TraceInformation("Snapshot saved", application, url);
        }

        /// <summary>
        /// Delete Snapshot
        /// </summary>
        /// <param name="application">The target container</param>
        /// <param name="url">The target url</param>
        /// <returns>bool</returns>
        public static void DeleteSnapshot(string application, string url)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(application);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(HttpUtility.UrlEncode(url));

            blockBlob.Delete();

            Trace.TraceInformation("Snapshot deleted", application, url);
        }

        /// <summary>
        /// Indicates if the snapshot already exists
        /// </summary>
        /// <param name="application">The target container</param>
        /// <param name="url">The url to check</param>
        /// <returns></returns>
        public static bool SnapshotExist(string application, string url)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(application);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(HttpUtility.UrlEncode(url));

            try
            {
                return blockBlob.Exists();
            }
            catch (StorageException ex)
            {
                Trace.TraceError(ex.Message, "AzureHelper","SnapshotExist","StorageException");
                return false;
            }
            
        }

        /// <summary>
        /// If the snapshot is expired the create a new one
        /// </summary>
        /// <param name="application">The target container</param>
        /// <param name="url">The url to check</param>
        /// <returns>true if expired</returns>
        public static bool SnapshotExpired(string application, string url)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(application);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(HttpUtility.UrlEncode(url));

            try
            {
                blockBlob.FetchAttributes();
            }
            catch (StorageException ex)
            {
                Trace.TraceError(ex.Message, "AzureHelper","SnapshotExpired","StorageException");
                return false;
            }
            
            //The snapshot, by default, will be available 3 days before being replaced by a new one
            return DateTime.Parse(blockBlob.Metadata["ExpirationDate"]) < DateTime.UtcNow;
        }

        /// <summary>
        /// Set Metadata properties
        /// </summary>
        /// <param name="application">The target container</param>
        /// <param name="url">The url to check</param>
        /// <param name="useragent">The user agent</param>
        /// <param name="expiration">The expiration date</param>
        public static void SetBlobAttributes(string application, string url, string useragent, DateTime expiration)
        {
            CloudBlobContainer container = BlobClient.GetContainerReference(application);

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(HttpUtility.UrlEncode(url));

            blockBlob.FetchAttributes();
            
            // Set Properties
            blockBlob.Properties.ContentType = "text/html";
            blockBlob.SetProperties();

            // Set Metadata
            blockBlob.Metadata["UserAgent"] = useragent;
            blockBlob.Metadata["Url"] = url;
            blockBlob.Metadata["ExpirationDate"] = expiration.ToString();
            blockBlob.SetMetadata();            
        }
    }
}
