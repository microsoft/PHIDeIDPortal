using System.Security.Claims;

namespace PhiDeidPortal.Ui.Services
{
    public interface IAuthorizationService
    {
        public bool Authorize(ClaimsPrincipal user);
    }
}
