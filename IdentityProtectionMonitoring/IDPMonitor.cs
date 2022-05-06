using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdentityProtectionMonitoring.Models;
using IdentityProtectionMonitoring.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace IdentityProtectionMonitoring
{
    public class IDPMonitor
    {
        private IConfiguration _config;
        private IGraphClientService _clientService;
        private AutotaskFunctions AutotaskFunctions;
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public IDPMonitor(IConfiguration config, IGraphClientService clientService)
        {
            _config = config;
            _clientService = clientService;
            AutotaskFunctions = new AutotaskFunctions();
        }

        [Function("IDPMonitor")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("IDPMonitor");

            // Check configuration
            if (string.IsNullOrEmpty(_config["IDP_Function_AppID"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_Secret"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_TenantID"]))
            {
                logger.LogError("Invalid app settings configured");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            // Is this a validation request?
            // https://docs.microsoft.com/graph/webhooks#notification-endpoint-validation
            if (executionContext.BindingContext.BindingData
                .TryGetValue("validationToken", out object? validationToken) && validationToken != null)
            {
                // Because validationToken is a string, OkObjectResult
                // will return a text/plain response body, which is
                // required for validation
                logger.LogInformation("Handling validation token");
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                response.WriteString(validationToken.ToString());
                return response;
            }

            // Not a validation request, process the body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            logger.LogInformation($"Change notification payload: {requestBody}");

            // Deserialize the JSON payload into a list of ChangeNotificationPayload
            // objects
            var notifications = JsonSerializer.Deserialize<NotificationList>(requestBody, jsonOptions);

            if (notifications != null)
            {
                var processNotificationsPayload = new ProcessNotificationsPayload()
                {
                    SecretKey = SharedFunctions.GetEnvironmentVariable("ProcessNotifications_SecretKey"),
                    NotificationList = notifications
                };
                string processNotificationsPayloadJson = JsonSerializer.Serialize(processNotificationsPayload);
                var processFunctionUri = req.Url.AbsoluteUri.Replace("IDPMonitor", "ProcessNotifications");
                processFunctionUri = Regex.Replace(processFunctionUri, @"\?code=\w+?==", "");

                // Send to ProcessNotifications endpoint to handle the new notifications
                // We do it this way so that we can immediately respond to the subscription
                // Microsoft requires us to respond within 3 seconds and process it takes longer
                var httpClient = new HttpClient();
                _ =  httpClient.PostAsync(
                    processFunctionUri,
                    new StringContent(processNotificationsPayloadJson, Encoding.UTF8, "application/json"));
            }

            // Return 202 per docs
            logger.LogInformation("Returning 202 response.");
            return req.CreateResponse(HttpStatusCode.Accepted);
        }
    }
}
