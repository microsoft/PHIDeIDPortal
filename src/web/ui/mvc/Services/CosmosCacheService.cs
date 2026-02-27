using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;

namespace PhiDeidPortal.Ui.Services
{
    public class CosmosCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly int _cacheExpirationMinutes;

        public CosmosCacheService(IDistributedCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
            _cacheExpirationMinutes = int.Parse(_configuration.GetSection("CosmosDb")["CacheExpirationMinutes"] ?? "90");
        }

        public string GetKeyPrefix(string key)
        {
            return _configuration.GetSection("CosmosDb")["CacheProviderUserPrefix"] ?? string.Empty;
        }

        async Task<string> ICacheService.GetStringAsync(string key)
        {
            return await _cache.GetStringAsync(key) ?? string.Empty;
        }

        async Task ICacheService.SetStringAsync(string key, string value)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheExpirationMinutes)
            };

            await _cache.SetStringAsync(key, value, options);
        }
    }
}
