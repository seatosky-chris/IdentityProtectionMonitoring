using IdentityProtectionMonitoring.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace IdentityProtectionMonitoring
{
    public class CreateUpdateSubscription
    {
        private IConfiguration _config;
        private IGraphClientService _clientService;
        private string subscriptionResource = "/security/alerts?$filter=status eq 'newAlert' and Severity ne 'Low'";
        private int expirationDays = 7;

        public CreateUpdateSubscription(IConfiguration config, IGraphClientService clientService)
        {
            _config = config;
            _clientService = clientService;
        }

        [Function("CreateUpdateSubscription")]
        public async Task Run(
            [TimerTrigger("0 0 0 * * *")] TimerInfo timerInfo,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CreateUpdateSubscription");
            logger.LogInformation($"Renewing subscription at: {DateTime.Now}");
            logger.LogInformation($"Next renewal scheduled for: {timerInfo.ScheduleStatus.Next}");

            // Check configuration
            if (string.IsNullOrEmpty(_config["IDP_Function_AppID"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_Secret"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_TenantID"]))
            {
                logger.LogError("Invalid app settings configured");
                return;
            }

            var notificationHost = SharedFunctions.GetEnvironmentVariable("Notification_Url");
            if (string.IsNullOrEmpty(notificationHost))
            {
                logger.LogError("Invalid notification host configured");
                return;
            }

            var clientState = SharedFunctions.GetEnvironmentVariable("Client_State_Secret");
            if (string.IsNullOrEmpty(clientState))
            {
                clientState = "DefaultClientState";
            }

            // Initialize a Graph client
            var graphClient = _clientService.GetAppGraphClient(logger);

            // Get any current subscriptions
            var existingSubs = await graphClient
                .Subscriptions
                .Request()
                .GetAsync();

            var createNewSub = true;

            Subscription? updatedOrCreatedSub = null;
            int updatedCount = 0;
            foreach (Subscription sub in existingSubs)
            {
                if (sub.ExpirationDateTime < DateTimeOffset.UtcNow)
                {
                    await graphClient
                        .Subscriptions[sub.Id]
                        .Request()
                        .DeleteAsync();
                }
                else if (sub.NotificationUrl == notificationHost)
                {
                    if (updatedCount < 1 && sub.Resource == subscriptionResource)
                    {
                        updatedCount++;
                        var updatedSub = new Subscription
                        {
                            ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(expirationDays)
                        };

                        updatedOrCreatedSub = await graphClient
                            .Subscriptions[sub.Id]
                            .Request()
                            .UpdateAsync(updatedSub);
                        logger.LogInformation($"Updated subscription '{sub.Id}' expiry time to {updatedSub.ExpirationDateTime}");
                        createNewSub = false;
                    }
                    else
                    {
                        await graphClient
                        .Subscriptions[sub.Id]
                        .Request()
                        .DeleteAsync();
                        logger.LogInformation($"Deleted extra subscription '{sub.Id}'");
                    }
                }
            }

            if (createNewSub)
            {
                var newSub = new Subscription
                {
                    ChangeType = "updated",
                    NotificationUrl = notificationHost,
                    Resource = subscriptionResource,
                    ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(expirationDays),
                    ClientState = clientState
                };
                updatedOrCreatedSub = await graphClient
                    .Subscriptions
                    .Request()
                    .AddAsync(newSub);
                logger.LogInformation($"Created new subscription expiring on {updatedOrCreatedSub.ExpirationDateTime}");
            }

            if (updatedOrCreatedSub == null)
            {
                logger.LogError("Could not update or create a new subscription.");
            }
            return;
        }
    }

    public class TimerInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
