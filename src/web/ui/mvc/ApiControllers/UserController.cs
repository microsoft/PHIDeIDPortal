using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using PhiDeidPortal.Ui.Entities;
using PhiDeidPortal.Ui.Services;
using System.Text;
using System.Text.Json;

namespace PhiDeidPortal.Ui.ApiControllers
{
    [ApiController]
    [Authorize]
    
    public class UserController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly Services.IAuthorizationService _authorizationService;

        public UserController(ICacheService cacheService, Services.IAuthorizationService authorizationService)
        {
            _cacheService = cacheService;
            _authorizationService = authorizationService;
        }

        [HttpPost]
        [Route("api/user/setconfig")]
        public async Task<IActionResult> SetUserConfiguration(UserConfiguration config)
        {
            var key = GetUserKey();
            if (string.IsNullOrWhiteSpace(key)) return Unauthorized();
            await _cacheService.SetStringAsync(key, JsonSerializer.Serialize(config));
            return Ok();
        }

        [HttpGet]
        [Route("api/user/getconfig")]
        public async Task<IActionResult> GetUserConfiguration()
        {
            var key = GetUserKey();
            if (string.IsNullOrWhiteSpace(key)) return Unauthorized();
            var config = await _cacheService.GetStringAsync(key);

            if (!String.IsNullOrWhiteSpace(config)) return Ok(JsonSerializer.Deserialize<UserConfiguration>(config));

            var environments = _authorizationService.GetAuthorizedEnvironments(User);
            if (environments.Count != 0)
            {
                config = JsonSerializer.Serialize(new UserConfiguration() { Environment = environments.First().EnvironmentName });
                await _cacheService.SetStringAsync(key, config);
                return Ok(JsonSerializer.Deserialize<UserConfiguration>(config));
            }

            return BadRequest();
        }
        private string GetUserKey()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username)) return string.Empty;
            return $"{_cacheService.GetKeyPrefix("user")}{username.ToLower()}";
        }

    }

}
