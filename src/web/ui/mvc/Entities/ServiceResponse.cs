using System.Net;

namespace PhiDeidPortal.Ui
{
    public class ServiceResponse
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode Code { get; set; }
        public string? Message { get; set; }
    }
}
