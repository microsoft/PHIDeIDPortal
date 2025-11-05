using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System.Net;

namespace PhiDeidPortal.Ui.Services
{
    public class BlobService : IBlobService
    {
        private BlobServiceClient _blobServiceClient;

        public BlobService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public BlobService(IConfiguration configuration)
        {
            var blobServiceConfiguration = configuration.GetSection("StorageAccount");
            var storageAccountUri = blobServiceConfiguration["Uri"];
            var managedIdentity = blobServiceConfiguration["UseManagedIdentity"];

            if (storageAccountUri == null)
            {
                throw new ArgumentNullException(nameof(storageAccountUri), "Storage account configuration is missing.");
            }

            if (managedIdentity is not null && managedIdentity.Equals("true", StringComparison.CurrentCultureIgnoreCase))
            {
                _blobServiceClient = new BlobServiceClient(new Uri(storageAccountUri), new DefaultAzureCredential());
                return;
            }

            var credential = new StorageSharedKeyCredential(blobServiceConfiguration["AccountName"], blobServiceConfiguration["AccountKey"]);
            _blobServiceClient = new BlobServiceClient(new Uri(storageAccountUri), credential);
        }

        public async Task<string> UploadDocumentAsync(IFormFile file, string containerName, string blobName)
        {
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

        public async Task<ServiceResponse> DeleteDocumentAsync(string containerName, string uri)
        {
            string blobName = $"{Path.GetFileName(uri)}";           
            
            var docBlobClient = _blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(blobName);

            var exists = docBlobClient.Exists();
            if (!exists) { return new ServiceResponse() { IsSuccess = false, Code = HttpStatusCode.NotFound, Message = "Document not found in the storage account" }; } 

            var delete = await docBlobClient.DeleteIfExistsAsync();
            return new ServiceResponse() { IsSuccess = delete.Value, Code = delete.Value == true ? HttpStatusCode.OK : HttpStatusCode.BadRequest, Message = delete.Value == true ? "Document deleted from the storage account" : "Document not deleted from storage account - BadRequest" };
        }

        public async Task<Uri> GetSasUriAsync(string containerName, string fileName)
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
