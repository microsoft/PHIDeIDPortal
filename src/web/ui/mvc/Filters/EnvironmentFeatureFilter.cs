using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using PhiDeidPortal.Ui.Entities;
using PhiDeidPortal.Ui.Services;
using System.Text.Json;
using IAuthorizationService = PhiDeidPortal.Ui.Services.IAuthorizationService;

namespace PhiDeidPortal.Ui.Filters;

[FilterAlias("EnvironmentFilter")]
public class EnvironmentFeatureFilter : IFeatureFilter
{
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EnvironmentFeatureFilter(
    ICacheService cacheService,
    IConfiguration configuration,
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor)
    {
        _cacheService = cacheService;
        _configuration = configuration;
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureContext)
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var username = user?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(username)) return false;

            var environments = _authorizationService.GetAuthorizedEnvironments(user);
            if (environments.Count == 0) return false;

            var key = $"{_cacheService.GetKeyPrefix("user")}{username.ToLower()}";
            var serializedConfig = await _cacheService.GetStringAsync(key);

            if (string.IsNullOrWhiteSpace(serializedConfig))
            {
                serializedConfig = JsonSerializer.Serialize(new UserConfiguration() { Environment = environments.First().EnvironmentName });
                await _cacheService.SetStringAsync(key, serializedConfig);
            }
            
            var config = JsonSerializer.Deserialize<UserConfiguration>(serializedConfig);
            var currentEnvironment = config?.Environment;

            var environment = environments.FirstOrDefault(e => e.EnvironmentName.Equals(currentEnvironment, StringComparison.OrdinalIgnoreCase));
            if (environment == null) return false;

            var featureName = featureContext.FeatureName;
            if (environment.Features.TryGetValue(featureName, out var isEnabled))
            {
                return isEnabled;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}