using LTWNC.Models;
using Microsoft.Extensions.Options;

namespace LTWNC.Services
{
    public interface IApiUrlService
    {
        string GetBaseUrl();
    }

    public class ApiUrlService : IApiUrlService
    {
        private readonly AppSettings _appSettings;

        public ApiUrlService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public string GetBaseUrl()
        {
            return _appSettings.BaseUrl;
        }
    }
} 