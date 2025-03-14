using Microsoft.Azure.Cosmos;
using PhiDeidPortal.Ui.Entities;

namespace PhiDeidPortal.Ui.Services
{
    public interface ICosmosService
    {
        Task<List<MetadataRecord>> QueryMetadataRecords(List<CosmosFieldQueryValue> fieldValues);
        StatusSummary GetSummary();
        StatusSummary GetSummaryByAuthor(string author);
        Task<ItemResponse<MetadataRecord>> UpsertMetadataRecordAsync(MetadataRecord record);
        Task<ServiceResponse> DeleteMetadataRecordAsync(MetadataRecord document);
    }

}