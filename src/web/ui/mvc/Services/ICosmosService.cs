using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui.Entities;

namespace PhiDeidPortal.Ui.Services
{
    public interface ICosmosService
    {
        List<MetadataRecord> GetAllMetadataRecords(string? searchString = null);
        List<MetadataRecord> GetAllMetadataRecordsByAuthor(string author, string? searchString = null);
        MetadataRecord? GetMetadataRecordById(string docId);
        MetadataRecord? GetMetadataRecordByUri(string uri);
        MetadataRecord? GetMetadataRecordByUriAndAuthor(string uri, string author);
        List<MetadataRecord> GetMetadataRecordsByStatus(int status, string? searchString = null);
        List<MetadataRecord> GetMetadataRecordsByStatusAndAuthor(int status, string author, string? searchString = null);
        StatusSummary GetSummary();
        StatusSummary GetSummaryByAuthor(string author);
        Task<ItemResponse<MetadataRecord>> UpsertMetadataRecordAsync(MetadataRecord record);
        Task<ServiceResponse> DeleteMetadataRecordAsync(MetadataRecord document);
    }

}