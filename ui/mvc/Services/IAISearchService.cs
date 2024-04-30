using Azure.Search.Documents.Models;
using Azure;

namespace PhiDeidPortal.Ui.Services
{
    public interface IAISearchService
    {
        Task<Pageable<SearchResult<SearchDocument>>> Query(string filter, string searchString = "*");
        Task<Pageable<SearchResult<SearchDocument>>> Query(string searchString = "*");
        Task<ServiceResponse> ResetDocument(string key);
        Task<ServiceResponse> RunIndexer(string name);
        Task<ServiceResponse> DeleteDocument(string key);
    }
}
