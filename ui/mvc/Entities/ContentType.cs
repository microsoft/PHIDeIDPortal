namespace PhiDeidPortal.Ui.Entities
{
    public static class AllowableContentType
    {
        public const string
            csv = "text/csv",
            doc = "application/msword",
            docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            json = "application/json",
            pdf = "application/pdf",
            txt = "text/plain",
            xls = "application/vnd.ms-excel",
            xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public static bool IsAllowable(string contentType)
        {
            return contentType == csv || contentType == doc || contentType == docx ||
                   contentType == json || contentType == pdf || contentType == txt ||
                   contentType == xls || contentType == xlsx;
        }
    }
}
