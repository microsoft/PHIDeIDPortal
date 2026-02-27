namespace PhiDeidPortal.Ui.Services
{
    public interface ICacheService
    {
        public Task<string> GetStringAsync(string key);
        public Task SetStringAsync(string key, string value);
        public string GetKeyPrefix(string key);
    }
}
