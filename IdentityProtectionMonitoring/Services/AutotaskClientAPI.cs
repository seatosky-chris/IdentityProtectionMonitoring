using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Services
{
    public class AutotaskClientAPI
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private ILogger? logger = null;

        /// <summary>
        /// Sends POST requests to the Autotask Client API (custom azure function)
        /// </summary>
        public AutotaskClientAPI()
        {
            var apiUrl = SharedFunctions.GetEnvironmentVariable("Autotask_ClientAPI_URL");
            var apiKey = SharedFunctions.GetEnvironmentVariable("Autotask_APIKey");

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                throw new ArgumentNullException("Autotask_ClientAPI_URL must be set.");
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException($"Autotask_APIKey must be set.");
            }

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _apiUrl = apiUrl;
        }

        public void SetLogger(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends a 'Get' type request to the requested endpoint
        /// </summary>
        /// <param name="endpoint">The name of the Autotask endpoint you are querying. E.g. Companies, Tickets, etc.</param>
        /// <param name="id">The id of the item to 'get'.</param>
        /// <returns>The object returned by the API</returns>
        public async Task<string?> Get(string endpoint, string id)
        {
            string requestJson = JsonSerializer.Serialize(new
            {
                endpoint,
                type = "get",
                id
            });
            return await this.SendRequest(requestJson);
        }

        /// <summary>
        /// Sends a 'Query' type request to the requested endpoint
        /// </summary>
        /// <param name="endpoint">The name of the Autotask endpoint you are querying. E.g. Companies, Tickets, etc.</param>
        /// <param name="filters">Optional, a JSON set of filters for the query.</param>
        /// <param name="includeFields">Optional, a JSON list of fields to include in the return object.</param>
        /// <returns>An array of the objects returned by the API</returns>
        public async Task<string?> Query(string endpoint, string? filters = null, string? includeFields = null)
        {
            string requestJson = JsonSerializer.Serialize(new
            {
                endpoint,
                type = "query",
                filters,
                includeFields
            });
            return await this.SendRequest(requestJson);
        }

        /// <summary>
        /// Sends a 'Count' type request to the requested endpoint
        /// </summary>
        /// <param name="endpoint">The name of the Autotask endpoint you are querying. E.g. Companies, Tickets, etc.</param>
        /// <param name="filters">Optional, a JSON set of filters for the query.</param>
        /// <returns>The object returned by the API, contains 1 key "queryCount"</returns>
        public async Task<string?> Count(string endpoint, string? filters = null)
        {
            string requestJson = JsonSerializer.Serialize(new
            {
                endpoint,
                type = "count",
                filters
            });
            return await this.SendRequest(requestJson);
        }

        /// <summary>
        /// Sends a 'Create' type request to the requested endpoint
        /// </summary>
        /// <param name="endpoint">The name of the Autotask endpoint you are querying. E.g. Companies, Tickets, etc.</param>
        /// <param name="payload">The JSON payload of the new object to create.</param>
        /// <returns>The object returned by the API, contains 1 key "id" which has the id of the newly created resource</returns>
        public async Task<string?> Create(string endpoint, string payload)
        {
            string requestJson = JsonSerializer.Serialize(new
            {
                endpoint,
                type = "create",
                payload
            });
            return await this.SendRequest(requestJson);
        }

        /// <summary>
        /// Sends a 'Update' type request to the requested endpoint
        /// </summary>
        /// <param name="endpoint">The name of the Autotask endpoint you are querying. E.g. Companies, Tickets, etc.</param>
        /// <param name="payload">The JSON payload of the changes to make to the resource. Only include those properties you want to change.</param>
        /// <returns>The object returned by the API, contains 1 key "id" which has the id of the updated resource</returns>
        public async Task<string?> Update(string endpoint, string payload)
        {
            string requestJson = JsonSerializer.Serialize(new
            {
                endpoint,
                type = "update",
                payload
            });
            return await this.SendRequest(requestJson);
        }

        private async Task<string?> SendRequest(string jsonContent)
        {
            try
            {
                var response = await _httpClient.PostAsync(
                        _apiUrl,
                        new StringContent(jsonContent, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                if (logger != null)
                {
                    logger.LogError("Could not send an API request to Autotask. Error: " + e + ". \nJSON content: " + jsonContent);
                }
                return null;
            }
        }
    }
}
