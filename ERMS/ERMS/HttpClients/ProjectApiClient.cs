// File: HttpClients/ProjectApiClient.cs

using System.Net.Http;
using System.Net.Http.Json; // For ReadFromJsonAsync etc.
using System.Threading.Tasks;
using ERMS.DTOs; // Your DTO namespace
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // For logging
using System; // For Uri

namespace ERMS.HttpClients // Adjust namespace if needed
{
    public class ProjectApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProjectApiClient> _logger; // Added Logger

        // Inject HttpClient (via factory), IConfiguration, and ILogger
        public ProjectApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ProjectApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Configure Base Address (adjust config keys if needed)
            var baseAddress = configuration["ApiSettings:BaseUrl"] ?? configuration["Jwt:Issuer"]; // Use API base or Issuer URL
            if (!string.IsNullOrEmpty(baseAddress))
            {
                if (!baseAddress.EndsWith('/')) baseAddress += "/"; // Ensure trailing slash
                _httpClient.BaseAddress = new Uri(baseAddress);
                _logger.LogInformation("ProjectApiClient BaseAddress set to: {BaseAddress}", _httpClient.BaseAddress);
            }
            else
            {
                _logger.LogWarning("API Base Address not found in configuration for ProjectApiClient.");
            }

            // Ensure standard headers if needed (e.g., accepting JSON)
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        // --- No AddAuthorizationHeader() method needed for this demo ---

        /// <summary>
        /// Gets all projects from the API.
        /// </summary>
        /// <returns>A list of ProjectDtos or null if an error occurs.</returns>
        public async Task<List<ProjectDto>?> GetProjectsAsync()
        {
            string requestUrl = "ProjectsApi"; // Relative URL
            _logger.LogInformation("ProjectApiClient making GET request to: {Url}", requestUrl);
            try
            {
                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call failed (GetProjectsAsync): {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    // Optionally read response body for more details if available
                    // var errorContent = await response.Content.ReadAsStringAsync();
                    // _logger.LogError("Error Content: {Content}", errorContent);
                    return null; // Indicate failure
                }

                // Ensure content exists before trying to deserialize
                if (response.Content == null || response.Content.Headers.ContentLength == 0)
                {
                    _logger.LogWarning("API call successful (GetProjectsAsync) but response content was null or empty.");
                    return new List<ProjectDto>(); // Return empty list for null/empty content
                }

                return await response.Content.ReadFromJsonAsync<List<ProjectDto>>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Exception in GetProjectsAsync to {Url}", requestUrl);
                return null;
            }
            catch (NotSupportedException ex) // JsonException is subclass
            {
                _logger.LogError(ex, "JSON Deserialization Exception in GetProjectsAsync from {Url}", requestUrl);
                return null;
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "Unexpected Exception in GetProjectsAsync to {Url}", requestUrl);
                return null;
            }
        }

        /// <summary>
        /// Gets a specific project by ID from the API.
        /// </summary>
        /// <param name="id">The project ID.</param>
        /// <returns>A ProjectDto or null if not found or an error occurs.</returns>
        public async Task<ProjectDto?> GetProjectByIdAsync(int id)
        {
            string requestUrl = $"ProjectsApi/{id}"; // Relative URL
            _logger.LogInformation("ProjectApiClient making GET request to: {Url}", requestUrl);
            try
            {
                var response = await _httpClient.GetAsync(requestUrl);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("API call returned 404 Not Found for Project ID {ProjectId}", id);
                    return null; // Treat 404 as null return
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call failed (GetProjectByIdAsync {ProjectId}): {StatusCode} - {ReasonPhrase}", id, response.StatusCode, response.ReasonPhrase);
                    return null; // Indicate failure
                }

                if (response.Content == null || response.Content.Headers.ContentLength == 0)
                {
                    _logger.LogWarning("API call successful (GetProjectByIdAsync {ProjectId}) but response content was null or empty.", id);
                    return null; // Return null if content is empty for a specific item request
                }

                return await response.Content.ReadFromJsonAsync<ProjectDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP Request Exception in GetProjectByIdAsync for ID {ProjectId} to {Url}", id, requestUrl);
                return null;
            }
            catch (NotSupportedException ex) // JsonException is subclass
            {
                _logger.LogError(ex, "JSON Deserialization Exception in GetProjectByIdAsync for ID {ProjectId} from {Url}", id, requestUrl);
                return null;
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "Unexpected Exception in GetProjectByIdAsync for ID {ProjectId} to {Url}", id, requestUrl);
                return null;
            }
        }

        // Add other methods (Create, Update, Delete) here if you intend to demonstrate
        // calling secured API endpoints from the MVC app (which would require solving
        // the authentication challenge or temporarily unsecuring those endpoints too).
        // For now, we only implement the GET methods needed for the demo.

    }
}