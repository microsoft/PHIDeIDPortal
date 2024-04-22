using Microsoft.Azure.Cosmos;

namespace PhiDeidPortal.Ui.Services
{
    public interface ICosmosService
    {
        MetadataRecord? GetMetadataRecord(string docId);
        Task<ItemResponse<MetadataRecord>> UpsertMetadataRecord(MetadataRecord record);
        MetadataRecord? GetMetadataRecordByUri(string uri);
    }
}