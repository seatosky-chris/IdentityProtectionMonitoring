using IdentityProtectionMonitoring.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Graph;

namespace IdentityProtectionMonitoring.Services
{
    // Service added via dependency injection
    // Used to get an authenticated Graph client
    public class GraphClientService : IGraphClientService
    {
        // Configuration
        private IConfiguration _config;

        private GraphServiceClient? _appGraphClient;

        public GraphClientService(IConfiguration config)
        {
            _config = config;
        }

        public GraphServiceClient GetAppGraphClient(ILogger logger)
        {
            if (_appGraphClient == null)
            {
                // Create a client credentials auth provider
                var authProvider = new ClientCredentialsAuthProvider(
                    _config["IDP_Function_AppID"],
                    _config["IDP_Function_Secret"],
                    _config["IDP_Function_TenantID"],
                    // The https://graph.microsoft.com/.default scope
                    // is required for client credentials. It requests
                    // all of the permissions that are explicitly set on
                    // the app registration
                    new[] { "https://graph.microsoft.com/.default" },
                    logger);

                _appGraphClient = new GraphServiceClient(authProvider);
            }

            return _appGraphClient;
        }
    }
}
