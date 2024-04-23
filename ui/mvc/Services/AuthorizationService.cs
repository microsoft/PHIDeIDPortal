using System.Security.Claims;

namespace PhiDeidPortal.Ui.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IConfigurationRoot _configuration;
        
        public AuthorizationService(IConfiguration configRoot)
        {
            _configuration = (IConfigurationRoot)configRoot;
        }

        public bool Authorize(ClaimsPrincipal user)
        {
            var userGroupClaim = user.Claims.FirstOrDefault(c => c.Type == "groups" && c.Value == _configuration.GetValue<string>("GroupClaimAdminId"));
            return userGroupClaim != null;
        }
    }
}
    