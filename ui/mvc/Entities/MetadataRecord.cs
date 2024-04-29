namespace PhiDeidPortal.Ui
{
    public record MetadataRecord(
        string id,
        string Uri,
        string FileName,
        int Status,
        string Author,
        string[] OrganizationalMetadata,
        string JustificationText,
        DateTime LastIndexed
    );
}
