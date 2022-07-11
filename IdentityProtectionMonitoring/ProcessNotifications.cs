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
using Microsoft.Graph;

namespace IdentityProtectionMonitoring
{
    public class ProcessNotifications
    {
        private IConfiguration _config;
        private IGraphClientService _clientService;
        private AutotaskFunctions AutotaskFunctions;
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// This can be used to connect different alert types within a ticket.
        /// If an alert type matches the key, it will search for existing tickets based on the alerts in the related list,
        /// if one is found, it will add a note rather than create a new ticket.
        /// </summary>
        private readonly Dictionary<string, List<string>> connectedAlertTypes = new()
        {
            { "Unfamiliar sign-in properties", new() { "Atypical travel", "Impossible travel" } },
            { "Atypical travel", new() { "Unfamiliar sign-in properties", "Impossible travel" } },
            { "Impossible travel", new() { "Atypical travel", "Unfamiliar sign-in properties" } }
        };

        public ProcessNotifications(IConfiguration config, IGraphClientService clientService)
        {
            _config = config;
            _clientService = clientService;
            AutotaskFunctions = new AutotaskFunctions();
        }

        [Function("ProcessNotifications")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("ProcessNotifications");

            // Check configuration
            if (string.IsNullOrEmpty(_config["IDP_Function_AppID"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_Secret"]) ||
                string.IsNullOrEmpty(_config["IDP_Function_TenantID"]))
            {
                logger.LogError("Invalid app settings configured");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            var clientState = SharedFunctions.GetEnvironmentVariable("Client_State_Secret");
            if (string.IsNullOrEmpty(clientState))
            {
                clientState = "DefaultClientState";
            }

            var secretKey = SharedFunctions.GetEnvironmentVariable("ProcessNotifications_SecretKey");
            if (string.IsNullOrEmpty(clientState))
            {
                logger.LogError("The 'ProcessNotifications_SecretKey' configuration is not set.");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var processNotificationsPayload = JsonSerializer.Deserialize<ProcessNotificationsPayload>(requestBody);
            NotificationList? notifications = null;

            // Validate secret key
            if (processNotificationsPayload == null || processNotificationsPayload.SecretKey != secretKey)
            {
                if (processNotificationsPayload == null)
                    logger.LogError($"Auth Error. Secret Key was not set.'");
                else
                    logger.LogError($"Auth Error. Invalid Secret Key used. '{processNotificationsPayload.SecretKey}'");
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            } 
            else
            {
                notifications = processNotificationsPayload.NotificationList;
            }

            // Process notifications
            if (notifications != null)
            {
                foreach (var notification in notifications.Value)
                {
                    if (notification.ClientState == clientState)
                    {
                        await ProcessAlertNotification(notification, logger);
                    }
                    else
                    {
                        logger.LogInformation($"Notification received with unexpected client state: {notification.ClientState}");
                    }
                }
            }

            logger.LogInformation("Processed all notifications.");
            return req.CreateResponse(HttpStatusCode.Accepted);
        }

        private async Task ProcessAlertNotification(ChangeNotificationPayload notification, ILogger logger)
        {
            string responseBody = "Failure! An unknown issue occurred."; // This will be overwritten if this successfully handles the alert

            // Initialize a Graph client
            var graphClient = _clientService.GetAppGraphClient(logger);

            var alertID = notification.ResourceData.Id;
            logger.LogInformation($"New alert detected. ID: {alertID} ChangeType: {notification.ChangeType} ClientState: {notification.ClientState} Resource: {notification.Resource}");

            // Get more info on the alert from the Graph API
            var alertInfoSearch = await graphClient
                .Security
                .Alerts
                .Request()
                .Filter($"id eq '{alertID}'")
                .GetAsync();
            var alertInfo = alertInfoSearch.FirstOrDefault();

            // Try to get info from the risk detection API
            var riskDetectionSearch = await graphClient
                .IdentityProtection
                .RiskDetections
                .Request()
                .Filter($"id eq '{alertID}'")
                .GetAsync();
            var riskDetection = riskDetectionSearch.FirstOrDefault();

            // Get risk detection history
            int? lastWeeksAlertCount = null;
            int? lastMonthsAlertCount = null;
            int? totalAlertCount = null;
            if (riskDetection != null)
            {
                var riskDetectionHistory = await graphClient
                    .IdentityProtection
                    .RiskDetections
                    .Request()
                    .Filter($"userPrincipalName eq '{riskDetection.UserPrincipalName}' and id ne '{alertID}'")
                    .OrderBy("activityDateTime desc")
                    .GetAsync();

                lastWeeksAlertCount = riskDetectionHistory.Where(rd => rd.ActivityDateTime > DateTime.Now.AddDays(-7)).Count();
                lastMonthsAlertCount = riskDetectionHistory.Where(rd => rd.ActivityDateTime > DateTime.Now.AddDays(-30)).Count();
                totalAlertCount = riskDetectionHistory.Count();
            }

            RiskyUser? riskyUser = null;
            if (riskDetection != null)
            {
                var riskyUserSearch = await graphClient
                    .IdentityProtection
                    .RiskyUsers
                    .Request()
                    .Filter($"userPrincipalName eq '{riskDetection.UserPrincipalName}'")
                    .GetAsync();
                riskyUser = riskyUserSearch.FirstOrDefault();
            }

            // ==================
            // Start Handling Alert
            // ==================
            logger.LogInformation("Start Handling Alert");
            if (alertInfo != null && riskDetection != null)
            {
                AlertDetails alertDetails = new()
                {
                    UserDisplayName = riskDetection.UserDisplayName,
                    Username = riskDetection.UserPrincipalName,
                    AadUserID = riskDetection.UserId,
                    UserRiskLevel = riskyUser != null && riskyUser.RiskLevel != null ? riskyUser.RiskLevel.ToString() : null,
                    UserRiskDetails = (riskyUser != null && riskyUser.RiskDetail != null ? riskyUser.RiskDetail.ToString() : null),
                    UserRiskReportId = riskyUser != null && riskyUser.Id != null ? riskyUser.Id : null,
                    AlertTitle = alertInfo.Title,
                    AlertCategory = alertInfo.Category,
                    AlertDescription = alertInfo.Description,
                    AlertEventDateTime = alertInfo.EventDateTime,
                    AlertSeverity = alertInfo.Severity.ToString(),
                    Activity = riskDetection.Activity.ToString(),
                    IpAddress = riskDetection.IpAddress,
                    Location = alertInfo.UserStates.First().LogonLocation,
                    RiskEventType = riskDetection.RiskEventType,
                    Source = riskDetection.Source,
                    AdditionalInfo = riskDetection.AdditionalInfo
                };
                logger.LogInformation(JsonSerializer.Serialize(alertDetails).ToString());

                // Ticket title (for creating new tickets and searching existing tickets)
                string ticketTitle = $"IDP Alert: '{alertDetails.AlertTitle}' for user '{alertDetails.UserDisplayName}'";

                // Search autotask for any existing and related tickets
                List<string> existingTicketFilters = new();
                existingTicketFilters.Add(ticketTitle);

                bool connectedAlertTypesFound = connectedAlertTypes.TryGetValue(alertDetails.AlertTitle, out List<string>? connectedAlertTypeTitles);
                if (connectedAlertTypesFound && connectedAlertTypeTitles != null && connectedAlertTypeTitles.Any())
                {
                    foreach (string alertTypeTitle in connectedAlertTypeTitles)
                    {
                        string connectedTicketTitle = $"IDP Alert: '{alertTypeTitle}' for user '{alertDetails.UserDisplayName}'";
                        existingTicketFilters.Add(connectedTicketTitle);
                    }
                }

                AutotaskTicketsList? existingTickets = null;
                try
                {
                    existingTickets = await AutotaskFunctions.SearchTickets(existingTicketFilters, titleSearchOperator: "eq", titleSearchTypeOr: true, openOnly: true);
                }
                catch (Exception ex)
                {
                    logger.LogError("Could not perform an Autotask ticket search.");
                }

                List<int>? existingTicketIds = null;
                if (existingTickets != null && existingTickets.Items != null)
                    existingTicketIds = existingTickets.Items.Select(t => t.Id).ToList();

                List<string> relatedTicketFilters = new();
                relatedTicketFilters.Add("IDP Alert:");
                relatedTicketFilters.Add($"for user '{alertDetails.UserDisplayName}'");
                var relatedTickets = await AutotaskFunctions.SearchTickets(relatedTicketFilters, lastXDays: 14, excludeTicketId: existingTicketIds);

                List<string?>? relatedTicketNumbers = null;
                if (relatedTickets != null && relatedTickets.Items != null)
                    relatedTicketNumbers = relatedTickets.Items.Select(t => t.TicketNumber).ToList();


                // ==================
                // Create / Update Ticket
                // ==================

                if (existingTickets != null && existingTickets.Items != null && existingTickets.Items.Count > 0)
                {
                    // UPDATE existing ticket
                    var existingTicket = existingTickets.Items.OrderByDescending(t => t.CreateDate).First();
                    string updateDescription = AutotaskFunctions.BuildAlertTicketDescription("Another IDP Risk Detection Alert was created. Details: ", alertDetails, relatedTicketNumbers);

                    AutotaskTicketNote newTicketNotePayload = new()
                    {
                        TicketID = existingTicket.Id,
                        Title = "New Alert Added",
                        Description = updateDescription
                    };

                    try
                    {
                        int? newTicketNoteID = await AutotaskFunctions.CreateTicketNote(newTicketNotePayload);
                        logger.LogInformation($"Updated ticket #{existingTicket.Id}");
                        responseBody = $"Updated ticket #{existingTicket.Id}";
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        responseBody = $"Failure! Could not update ticket #{existingTicket.Id}";
                    }
                }
                else
                {
                    // CREATE a new ticket
                    string orgID = SharedFunctions.GetEnvironmentVariable("Autotask_OrgID");

                    // Get primary location, contract, and try to find the effected user's contact
                    int? contractID = await AutotaskFunctions.GetPrimaryContract();
                    int locationID = await AutotaskFunctions.GetPrimaryLocation();
                    int? contactID = await AutotaskFunctions.GetRelatedContact(riskDetection);

                    // Make a new ticket
                    string ticketDescriptionIntro = "A new IDP Risk Detection Alert was created.";
                    string? ticketDescriptionFooter = null;
                    if (lastWeeksAlertCount != null && lastMonthsAlertCount != null && totalAlertCount != null)
                    {
                        if (lastWeeksAlertCount == 0 && lastMonthsAlertCount == 0 && totalAlertCount == 0)
                            ticketDescriptionFooter = "No previous risk detections were found!";
                        else
                        {
                            ticketDescriptionFooter = "Other Risk Detections Alerts in the last:\n";
                            ticketDescriptionFooter += $@"Week: {lastWeeksAlertCount}
Month: {lastMonthsAlertCount}
Total: {totalAlertCount}";
                        }
                    }
                    string ticketDescription = AutotaskFunctions.BuildAlertTicketDescription(ticketDescriptionIntro, alertDetails, relatedTicketNumbers, ticketDescriptionFooter);


                    AutotaskNewTicket newTicketPayload = new()
                    {
                        CompanyID = orgID != null ? Int32.Parse(orgID) : 0,
                        CompanyLocationID = locationID,
                        Priority = 1,
                        Status = 1,
                        QueueID = 8,
                        IssueType = 31,
                        SubIssueType = 222,
                        ServiceLevelAgreementID = 5,
                        ContractID = contractID,
                        Title = ticketTitle,
                        Description = ticketDescription
                    };

                    int? newTicketID = null;
                    try
                    {
                        newTicketID = await AutotaskFunctions.CreateTicket(newTicketPayload);
                        responseBody = $"Created new ticket #{newTicketID}";
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        responseBody = $"Failure! Could not create a new ticket.";
                    }

                    if (newTicketID != null)
                        logger.LogInformation("New ticket created: " + newTicketID.ToString());
                    else
                        logger.LogError("Creating new ticket failed! Title of would be ticket: " + ticketTitle);

                }
            }
            else if (alertInfo != null)
            {
                // Alert me that this is a possible type to create tickets for
                string emailEndpoint = SharedFunctions.GetEnvironmentVariable("Email_APIEndpoint");
                string emailApiKey = SharedFunctions.GetEnvironmentVariable("Email_APIKey");
                string emailFrom_Email = SharedFunctions.GetEnvironmentVariable("Email_From__Email");
                string emailFrom_Name = SharedFunctions.GetEnvironmentVariable("Email_From__Name");
                string emailTo_Email = SharedFunctions.GetEnvironmentVariable("Email_To__Email");
                string emailTo_Name = SharedFunctions.GetEnvironmentVariable("Email_To__Name");

                if (emailEndpoint != null && emailApiKey != null && emailFrom_Email != null && emailTo_Email != null)
                {
                    string emailBody = $"<strong>Unhandled Alert Type Found:</strong> {alertInfo.Category}";
                    emailBody += $"\n<strong>Title:</strong> {alertInfo.Title} \n<strong>Description:</strong> {alertInfo.Description}";
                    emailBody += $"\n<strong>From Domain:</strong> {alertInfo.UserStates.First().DomainName}";
                    emailBody += $"\n\n<strong>Full Alert JSON:</strong> {JsonSerializer.Serialize(alertInfo)}";
                    emailBody = Regex.Replace(emailBody, "\r?\n", "<br />");

                    NewEmail newEmail = new()
                    {
                        From = new From
                        {
                            Email = emailFrom_Email,
                            Name = emailFrom_Name
                        },
                        To = new List<To>
                            {
                                new To {
                                    Email = emailTo_Email,
                                    Name= emailTo_Name
                                }
                            },
                        Subject = $"Possible New Alert Type for IDP Monitoring: {alertInfo.Category}",
                        HTMLContent = emailBody
                    };
                    string jsonEmail = JsonSerializer.Serialize(newEmail);

                    // Send email
                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("x-api-key", emailApiKey);
                    var emailSendResponse = await httpClient.PostAsync(
                        emailEndpoint,
                        new StringContent(jsonEmail, Encoding.UTF8, "application/json"));

                    logger.LogInformation($"Sent email for new type: {alertInfo.Category}");
                    responseBody = "Alert not handled. Email sent with alert type.";
                }
            }
            else
            {
                // Something went wrong
                responseBody = $"Failure! Alert #'{alertID}' could not be handled.";
                logger.LogError(responseBody);
            }

            if (responseBody.StartsWith("Failure!"))
                logger.LogError(responseBody);
            else
                logger.LogInformation(responseBody);

            logger.LogInformation("Finished Processing Alert");
        }
    }
}
