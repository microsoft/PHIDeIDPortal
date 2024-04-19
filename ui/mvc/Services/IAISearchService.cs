using Azure.Search.Documents.Models;
using Azure;

namespace PhiDeidPortal.Ui.Services
{
    public interface IAISearchService
    {
        Task<Pageable<SearchResult<SearchDocument>>> Query(string filter, string searchString = "*");
        Task<Pageable<SearchResult<SearchDocument>>> Query(string searchString = "*");
        Task<bool> ResetDocument(string key);
        Task<bool> RunIndexer(string name);
        Task<bool> DeleteDocument(string key);
    }
}
