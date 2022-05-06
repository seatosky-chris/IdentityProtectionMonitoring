using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityProtectionMonitoring.Authentication;
using IdentityProtectionMonitoring.Models;
using IdentityProtectionMonitoring.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace IdentityProtectionMonitoring.Models
{
    public class SetAlertSubscription
    {
        private IConfiguration _config;
        private IGraphClientService _clientService;

        public SetAlertSubscription(IConfiguration config, IGraphClientService clientService)
        {
            _config = config;
            _clientService = clientService;
        }

        [Function("SetAlertSubscription")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("SetAlertSubscription");

            // Check configuration
            if (string.IsNullOrEmpty(_config["IDP_Function_AppID"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_Secret"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_TenantID"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_TenantID"]))
            {
                logger.LogError("Invalid app settings configured");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            var notificationHost = SharedFunctions.GetEnvironmentVariable("Notification_Url");
            if (string.IsNullOrEmpty(notificationHost))
            {
                logger.LogError("Invalid notification host configured");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            // Initialize Graph client
            var graphClient = _clientService.GetAppGraphClient(logger);

            // Create a new subscription object
            var subscription = new Subscription
            {
                ChangeType = "created,updated",
                NotificationUrl = notificationHost,
                Resource = "/security/alerts?$filter=status eq 'newAlert'",
                ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(2),
                ClientState = "test"
            };

            var createdSub = await graphClient
                .Subscriptions
                .Request()
                .AddAsync(subscription);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync<Subscription>(createdSub);
            return response;
        }
    }
}
