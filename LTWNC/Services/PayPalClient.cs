using LTWNC.Models;
using Microsoft.Extensions.Options;
using PayPalCheckoutSdk.Core;
using PayPalHttp;

namespace LTWNC.Services
{
    public class PayPalClient
    {
        private readonly PayPalSettings _settings;

        public PayPalClient(IOptions<PayPalSettings> options)
        {
            _settings = options.Value;
        }

        public PayPalHttpClient Client()
        {
            PayPalEnvironment environment = _settings.Mode == "live"
                ? new LiveEnvironment(_settings.ClientId, _settings.ClientSecret)
                : new SandboxEnvironment(_settings.ClientId, _settings.ClientSecret);

            return new PayPalHttpClient(environment);
        }
    }
}
