
using Azure;
using Microsoft.AspNetCore.Mvc;

namespace PhiDeidPortal.Ui.Services
{
    public interface IBlobService
    {
        Task<string> UploadDocumentAsync(IFormFile file, string containerName, string blobName);
        Task<Stream> GetDocumentStreamAsync(string containerName, string fileName);
        Task<Uri> GetSasUriAsync(string containerName, string fileName);
        Task<ServiceResponse> DeleteDocumentAsync(string containerName, string uri);
    }
}