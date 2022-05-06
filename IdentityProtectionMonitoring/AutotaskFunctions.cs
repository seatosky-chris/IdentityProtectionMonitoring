using System.Text.Json;
using IdentityProtectionMonitoring.Models;
using IdentityProtectionMonitoring.Services;
using Microsoft.Graph;

namespace IdentityProtectionMonitoring
{
    public class AutotaskFunctions
    {
        private AutotaskClientAPI AutotaskAPI;
        private static readonly JsonSerializerOptions jsonSerializerDefaults = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AutotaskFunctions()
        {
            AutotaskAPI = new AutotaskClientAPI();
        }

        /// <summary>
        /// Gets the primary location for this company and returns the ID
        /// </summary>
        /// <returns>The primary locations ID</returns>
        public async Task<int> GetPrimaryLocation()
        {
            int locationID = 10; // Default value is 10

            AutotaskLocation? location = null;
            string includeFields = JsonSerializer.Serialize(new[]
                {
                    "id", "isActive", "isPrimary"
                });

            string locationsJson = await AutotaskAPI.Query("CompanyLocations", null, includeFields);
            AutotaskLocationsList? locations = JsonSerializer.Deserialize<AutotaskLocationsList>(locationsJson, jsonSerializerDefaults);

            if (locations != null && SharedFunctions.HasProperty(locations, "Items") && locations.Items != null)
            {
                location = locations.Items.Where(l => l.IsPrimary).FirstOrDefault();
                if (location == null)
                {
                    location = locations.Items.Where(l => l.IsActive).FirstOrDefault();
                }
            }
            if (location != null && location.Id > 0)
            {
                locationID = location.Id;
            }

            return locationID;
        }

        /// <summary>
        /// Gets the primary contract for this company and returns the ID
        /// </summary>
        /// <returns>The primary contract ID if found, otherwise null</returns>
        public async Task<int?> GetPrimaryContract()
        {
            int? contractID = null; // Defaults to null

            string atFilter = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        op = "eq",
                        field = "IsDefaultContract",
                        value = true
                    }
                });
            var includeFields = JsonSerializer.Serialize(new[]
                {
                    "id"
                });

            string contractsJson = await AutotaskAPI.Query("Contracts", atFilter, includeFields);
            AutotaskContractsList? contracts = JsonSerializer.Deserialize<AutotaskContractsList>(contractsJson, jsonSerializerDefaults);

            if (contracts != null && SharedFunctions.HasProperty(contracts, "Items") && contracts.Items != null)
            {
                contractID = contracts.Items.First().Id; 
            }

            return contractID;
        }

        /// <summary>
        /// Gets a Autotask Contact that is related to a new risk detection
        /// Searches by email address and name
        /// </summary>
        /// <param name="riskDetection">The risk detection entry returned by the microsoft graph API.</param>
        /// <returns>The best matching contacts ID, if none are found, returns null</returns>
        public async Task<int?> GetRelatedContact(RiskDetection riskDetection)
        {
            int? contactID = null; // Default to null

            string firstNamePart = riskDetection.UserDisplayName.Split(' ').First();
            string lastNamePart = riskDetection.UserDisplayName.Split(' ').Last();

            var atFilter = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        op = "or",
                        items = new[]
                        {
                            new
                            {
                                op = "contains",
                                field = "emailAddress",
                                value = riskDetection.UserPrincipalName
                            },
                            new
                            {
                                op = "contains",
                                field = "emailAddress2",
                                value = riskDetection.UserPrincipalName
                            },
                            new
                            {
                                op = "contains",
                                field = "emailAddress3",
                                value = riskDetection.UserPrincipalName
                            },
                            new
                            {
                                op = "contains",
                                field = "firstName",
                                value = firstNamePart
                            },
                            new
                            {
                                op = "contains",
                                field = "lastName",
                                value = lastNamePart
                            },
                        }
                    }
                });
            var includeFields = JsonSerializer.Serialize(new[]
                {
                    "id", "emailAddress", "emailAddress2", "emailAddress3", "firstName", "lastName", "isActive", "lastActivityDate"
                });

            string contactsJson = await AutotaskAPI.Query("Contacts", atFilter, includeFields);
            AutotaskContactsList? contacts = JsonSerializer.Deserialize<AutotaskContactsList>(contactsJson, jsonSerializerDefaults);


            // Filter down to the best contact
            if (contacts != null && SharedFunctions.HasProperty(contacts, "Items") && contacts.Items != null)
            {
                var allContacts = contacts.Items.Where(c => c.IsActive == 1).ToList();

                if (allContacts.Any())
                {
                    bool filtersApplied = false;
                    if (allContacts.Count() > 1)
                    {
                        var tempContacts = allContacts.Where(c =>
                            (c.EmailAddress != null && c.EmailAddress.Contains(riskDetection.UserPrincipalName)) ||
                            (c.EmailAddress2 != null && c.EmailAddress2.Contains(riskDetection.UserPrincipalName)) ||
                            (c.EmailAddress3 != null && c.EmailAddress3.Contains(riskDetection.UserPrincipalName))).ToList();

                        if (tempContacts.Any())
                        {
                            allContacts = tempContacts.ToList();
                            filtersApplied = true;
                        }
                    }

                    if (allContacts.Count() > 1)
                    {
                        var tempContacts = allContacts.Where(c =>
                            (c.EmailAddress != null && c.EmailAddress.Contains(riskDetection.UserPrincipalName))).ToList();

                        if (tempContacts.Any())
                        {
                            allContacts = tempContacts.ToList();
                            filtersApplied = true;
                        }
                    }

                    if (allContacts.Count() > 1)
                    {
                        var tempContacts = allContacts.Where(c =>
                            (c.FirstName != null && c.FirstName.Contains(firstNamePart)) &&
                            (c.LastName != null && c.LastName.Contains(lastNamePart))).ToList();

                        if (tempContacts.Any())
                        {
                            allContacts = tempContacts.ToList();
                            filtersApplied = true;
                        }
                    }

                    if (allContacts.Count() > 1)
                    {
                        var tempContacts = allContacts.Where(c =>
                            ((c.EmailAddress != null && c.EmailAddress.Contains(riskDetection.UserPrincipalName)) ||
                            (c.EmailAddress2 != null && c.EmailAddress2.Contains(riskDetection.UserPrincipalName)) ||
                            (c.EmailAddress3 != null && c.EmailAddress3.Contains(riskDetection.UserPrincipalName))) &&
                            ((c.FirstName != null && c.FirstName.Contains(firstNamePart)) ||
                            (c.LastName != null && c.LastName.Contains(lastNamePart)))).ToList();

                        if (tempContacts.Any())
                        {
                            allContacts = tempContacts.ToList();
                            filtersApplied = true;
                        }
                    }

                    if ((filtersApplied && allContacts.Count() > 1) || allContacts.Count() == 1)
                    {
                        var contact = allContacts.OrderByDescending(c => c.LastActivityDate).FirstOrDefault();
                        if (contact != null)
                            contactID = contact.Id;
                    }
                }
            }

            return contactID;
        }

        /// <summary>
        /// Gets a list of tickets based on various search parameters
        /// </summary>
        /// <param name="titleSearch">A list of strings that will be searched in the tickets title, all must be found for a match.</param>
        /// <param name="titleSearchOperator">The type of search to perform on ticket titles. Defaults to "contains" but "eq" is another good option.</param>
        /// <param name="openOnly">If true, filters by tickets that are still open.</param>
        /// <param name="lastXDays">If set, filters by tickets that were created in the past X days.</param>
        /// <param name="excludeTicketId">A list of ticket IDs to exclude, ignored if set to null.</param>
        /// <returns>The list of tickets that were found, if none, then null</returns>
        public async Task<AutotaskTicketsList?> SearchTickets(List<string> titleSearch, string titleSearchOperator = "contains", bool openOnly = false, double? lastXDays = null, List<int>? excludeTicketId = null)
        {
            List<AutotaskQueryFilterItem> filters = new();

            foreach (string titlePart in titleSearch)
            {
                filters.Add(new AutotaskQueryFilterItem
                {
                    op = titleSearchOperator,
                    field = "title",
                    value = titlePart
                });
            }
            if (openOnly)
            {
                filters.Add(new AutotaskQueryFilterItem
                {
                    op = "notExist",
                    field = "CompletedByResourceID"
                });
                filters.Add(new AutotaskQueryFilterItem
                {
                    op = "notExist",
                    field = "CompletedDate"
                });
            }
            if (lastXDays != null)
            {
                DateTime greaterThanDateTime = DateTime.Now.AddDays(-1 * (double)lastXDays);
                filters.Add(new AutotaskQueryFilterItem
                { 
                    op = "gt",
                    field = "createDate",
                    value = greaterThanDateTime.Date.ToString()
                });
            }
            if (excludeTicketId != null)
            {
                foreach (int ticketId in excludeTicketId)
                {
                    filters.Add(new AutotaskQueryFilterItem
                    {
                        op = "noteq",
                        field = "id",
                        value = ticketId.ToString()
                    });
                }
            }

            string atFilter = JsonSerializer.Serialize(new[]
            {
                new
                {
                    op = "and",
                    items = filters.ToArray()
                }
            });

            string ticketsJson = await AutotaskAPI.Query("Tickets", atFilter);
            AutotaskTicketsList? tickets = JsonSerializer.Deserialize<AutotaskTicketsList>(ticketsJson, jsonSerializerDefaults);

            return tickets;
        }

        /// <summary>
        /// Creates a new ticket in Autotask
        /// </summary>
        /// <param name="newTicketPayload">The payload for creating the new ticket.</param>
        /// <returns>The ID of the newly created ticket if successful, otherwise null.</returns>
        public async Task<int?> CreateTicket(AutotaskNewTicket newTicketPayload)
        {
            int? newTicketId = null; // Default is null, if it returns null then creating a ticket did not work

            var newTicketPayloadJson = JsonSerializer.Serialize(newTicketPayload);
            string newTicketJson = await AutotaskAPI.Create("Tickets", newTicketPayloadJson);
            AutotaskCreatedResource? newTicket = JsonSerializer.Deserialize<AutotaskCreatedResource>(newTicketJson, jsonSerializerDefaults);

            if (newTicket != null)
            {
                newTicketId = newTicket.ItemId;
            }
            else
            {
                throw new InvalidOperationException("Ticket Creation Failed. Error: " + newTicketJson);
            }

            return newTicketId;
        }

        /// <summary>
        /// Creates a new ticket note in Autotask.
        /// This will additionally update the ticket status to "Waiting Helpdesk".
        /// </summary>
        /// <param name="newTicketNotePayload">The payload for creating the new ticket note.</param>
        /// <returns>The ID of the newly created ticket note if successful, otherwise null.</returns>
        public async Task<int?> CreateTicketNote(AutotaskTicketNote newTicketNotePayload)
        {
            int? newTicketNoteId = null; // Default is null, if it returns null then creating a ticket note did not work

            var newTicketNotePayloadJson = JsonSerializer.Serialize(newTicketNotePayload);
            string newTicketNoteJson = await AutotaskAPI.Create("TicketNotes", newTicketNotePayloadJson);
            AutotaskCreatedResource? ticketNote = JsonSerializer.Deserialize<AutotaskCreatedResource>(newTicketNoteJson, jsonSerializerDefaults);

            if (ticketNote != null)
            {
                newTicketNoteId = ticketNote.ItemId;

                // Change ticket status to waiting helpdesk
                var ticketUpdateJson = JsonSerializer.Serialize(new AutotaskUpdateTicketStatus
                {
                    Id = newTicketNotePayload.TicketID,
                    Status = 21
                });
                await AutotaskAPI.Update("Tickets", ticketUpdateJson);
            }
            else
            {
                throw new InvalidOperationException("Ticket Note Creation Failed. Error: " + newTicketNoteJson);
            }

            return newTicketNoteId;
        }

        /// <summary>
        /// A helper function that builds and formats the description of a ticket.
        /// </summary>
        /// <param name="intro">An intro for the description, this is generally one line at the top of the ticket.</param>
        /// <param name="alertDetails">An AlertDetails object containing the alert info, this will be used to create the main body of the description.</param>
        /// <param name="relatedTickets">A list of related ticket numbers to link to in the description. Ignored if null.</param>
        /// <param name="footer">A custom footer for the description, you can add any additional info with this. Ignored if null.</param>
        /// <returns>Returns the new description as a string.</returns>
        public static string BuildAlertTicketDescription(string intro, AlertDetails alertDetails, List<string?>? relatedTickets = null, string? footer = null)
        {
            string ticketDescription =
                        @$"{intro}

Type: {alertDetails.AlertTitle} [{alertDetails.AlertCategory} / {alertDetails.RiskEventType}]
Triggered by: {alertDetails.Activity} (Severity: {alertDetails.AlertSeverity})
Description: {alertDetails.AlertDescription}

User: {alertDetails.UserDisplayName} ({alertDetails.Username})";

            if (alertDetails.UserRiskLevel != null && alertDetails.UserRiskDetails != null)
            {
                ticketDescription += $"\nUsers Risk Level: {alertDetails.UserRiskLevel}";
                if (alertDetails.UserRiskDetails != "None")
                    ticketDescription += $" ({alertDetails.UserRiskDetails})";
            }

            ticketDescription += $"\nLogon Location: {alertDetails.Location} (IP: {alertDetails.IpAddress})";

            if (alertDetails.AlertEventDateTime != null)
                ticketDescription += "\nWhen: " + alertDetails.AlertEventDateTime.Value.LocalDateTime.ToString();
            ticketDescription += $"\n\nAlert Source: {alertDetails.Source}";
            ticketDescription += $"\nAdditional Info: {alertDetails.AdditionalInfo}";

            if (alertDetails.UserRiskReportId != "None")
                ticketDescription += $"\nUser Risk Details: https://portal.azure.com/#blade/Microsoft_AAD_IAM/RiskyUsersBlade/userId/{alertDetails.UserRiskReportId}";

            if (relatedTickets != null && relatedTickets.Count > 0)
            {
                relatedTickets.RemoveAll(string.IsNullOrEmpty);
                ticketDescription += "\nRelated Tickets: ";
                ticketDescription += string.Join(", ", relatedTickets);
            }

            if (footer != null)
                ticketDescription += $"\n\n{footer}";

            return ticketDescription;
        }
    }
}
