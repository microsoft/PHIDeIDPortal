using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PhiDeidPortal.Ui.Services
{
    public class BlobService : IBlobService
    {
        private BlobServiceClient _blobServiceClient;

        public BlobService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task<string> UploadDocumentAsync(IFormFile file, string containerName)
        {
            string blobName = $"{Path.GetFileName(file.FileName)}";

            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            using (Stream stream = file.OpenReadStream())
            {
                await containerClient.UploadBlobAsync(blobName, stream);
            }

            var blobClient = containerClient.GetBlobClient(blobName);

            return blobClient.Uri.ToString();
        }

        public async Task<Stream> GetDocumentStreamAsync(string containerName, string fileName)
        {
            var docBlobClient = _blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(fileName);

            var blobStream = await docBlobClient.OpenReadAsync();

            return blobStream;
        }

        public async Task<Uri> GetSasUri(string containerName, string fileName)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(fileName); // blob name
                                                                             // Get a user delegation key for the Blob service that's valid for 2 hours.
            var userDelegationKey = _blobServiceClient.GetUserDelegationKey(DateTimeOffset.UtcNow,
                                                                            DateTimeOffset.UtcNow.AddHours(2));
            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b", // b for blob, c for container
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(2),
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read); // read permissions
                                                                // Add the SAS token to the container URI.
            var blobUriBuilder = new BlobUriBuilder(_blobServiceClient.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, _blobServiceClient.AccountName)
            };

            return blobUriBuilder.ToUri();
        }
    }
}
