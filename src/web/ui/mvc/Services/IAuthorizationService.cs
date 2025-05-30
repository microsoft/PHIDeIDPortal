using System.Security.Claims;

namespace PhiDeidPortal.Ui.Services
{
    public interface IAuthorizationService
    {
        bool HasElevatedRights(ClaimsPrincipal user);
    }
}
