namespace PhiDeidPortal.Ui
{
    public record MetadataRecord(
        string id,
        string Author,
        bool AwaitingIndex,
        string Environment,
        string FileName,
        string JustificationText,
        DateTime LastIndexed,
        string[] OrganizationalMetadata,
        int Status,
        string Uri
    );
}
