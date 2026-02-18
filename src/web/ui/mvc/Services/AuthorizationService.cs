using Azure.Storage.Blobs.Models;
using PhiDeidPortal.Ui.Entities;
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

        public bool HasElevatedRights(ClaimsPrincipal user)
        {
            var adminGroupId = _configuration.GetValue<string>("GroupClaimAdminId");
            if (string.IsNullOrWhiteSpace(adminGroupId))
            {
                return false;
            }

            var userGroupClaims = GetUserGroupClaims(user);
            return userGroupClaims.Any(c => c == adminGroupId);
        }

        public List<EnvironmentGroupClaim> GetAuthorizedEnvironments(ClaimsPrincipal user)
        {
            var environments = _configuration.GetSection("EnvironmentGroupClaims").Get<List<EnvironmentGroupClaim>>() ?? [];
            var groups = GetUserGroupClaims(user);
            if (groups.Count == 0) return [];
            environments = environments.Where(env => env.GroupClaimIds.Any(gid => groups.Contains(gid))).OrderBy(x => x.EnvironmentName).ToList();
            return environments;
        }

        private static List<string> GetUserGroupClaims(ClaimsPrincipal user)
        {
            return user.Claims.Where(c => c.Type == "groups").Select(c => c.Value).ToList();
        }
    }
}