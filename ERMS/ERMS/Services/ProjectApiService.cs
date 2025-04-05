using ERMS.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ERMS.Services
{
    public class ProjectApiService : ApiService
    {
        public ProjectApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
            : base(httpClient, httpContextAccessor, configuration)
        {
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync("ProjectsApi");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Project>>() ?? new List<Project>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("Unauthorized: The user is not authenticated.");
                    throw new UnauthorizedAccessException("You are not authorized to access this resource.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error fetching projects: {response.StatusCode}, Content: {errorContent}");
                    throw new HttpRequestException($"Error fetching projects: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetProjectsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Project?> GetProjectAsync(int id)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.GetAsync($"ProjectsApi/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Project>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Content: {errorContent}");
                    throw new HttpRequestException($"Error fetching project {id}: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetProjectAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Project?> CreateProjectAsync(Project project)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.PostAsJsonAsync("ProjectsApi", project);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Project>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Content: {errorContent}");
                    throw new HttpRequestException($"Error creating project: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in CreateProjectAsync: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateProjectAsync(int id, Project project)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.PutAsJsonAsync($"ProjectsApi/{id}", project);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Content: {errorContent}");
                    throw new HttpRequestException($"Error updating project {id}: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateProjectAsync: {ex.Message}");
                throw;
            }
        }
        protected void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        public async Task DeleteProjectAsync(int id)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _httpClient.DeleteAsync($"ProjectsApi/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Content: {errorContent}");
                    throw new HttpRequestException($"Error deleting project {id}: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteProjectAsync: {ex.Message}");
                throw;
            }
        }
    }
}
