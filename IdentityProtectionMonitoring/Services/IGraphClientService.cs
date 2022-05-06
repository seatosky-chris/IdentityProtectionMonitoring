using IdentityProtectionMonitoring.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace IdentityProtectionMonitoring.Services
{
    public interface IGraphClientService
    {
        GraphServiceClient GetAppGraphClient(ILogger logger);
    }
}
