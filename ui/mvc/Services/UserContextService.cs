using System.Security.Claims;

namespace PhiDeidPortal.Ui.Services
{
    public class UserContextService : IUserContextService
    {
        public ClaimsPrincipal User { get; set; }
        public bool HasElevatedRights { get; set; }
        public bool ViewFilter { get; set; }
    }
}
