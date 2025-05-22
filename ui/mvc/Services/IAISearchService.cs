using Azure.Search.Documents.Models;
using Azure;

namespace PhiDeidPortal.Ui.Services
{
    public interface IAISearchService
    {
        Task<Pageable<SearchResult<SearchDocument>>> SearchAsync(string? filter, string? searchString = "*");
        Task<Pageable<SearchResult<SearchDocument>>> SearchByAuthorAsync(string author, string? filter, string? searchString = "*");
        Task<ServiceResponse> ResetDocumentAsync(string key);
        Task<ServiceResponse> RunIndexerAsync(string name);
        Task<ServiceResponse> DeleteDocumentAsync(string key);
        Task<List<string>> GetFailedIndexedFiledAsync(string name);
    }
}
