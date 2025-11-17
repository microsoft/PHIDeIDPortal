
using Azure;
using Microsoft.AspNetCore.Mvc;

namespace PhiDeidPortal.Ui.Services
{
    public interface IBlobService
    {
        Task<string> UploadDocumentAsync(string container, string blob, IFormFile file);
        Task<Stream> GetDocumentStreamAsync(string container, string blob);
        Task<Uri> GetSasUriAsync(string container, string blob);
        Task<ServiceResponse> SetBlobUserDefinedMetadataAsync(string container, string blob, IDictionary<string, string> metadata);
        Task<ServiceResponse> DeleteDocumentAsync(string container, string uri);
    }
}