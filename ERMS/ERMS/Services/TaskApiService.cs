using ERMS.Models;
using System.Net.Http.Json;

namespace ERMS.Services
{
    public class TaskApiService : ApiService
    {
        public TaskApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
            : base(httpClient, httpContextAccessor, configuration)
        {
        }

        public async Task<List<ProjectTask>> GetTasksAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync("TasksApi");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ProjectTask>>() ?? new List<ProjectTask>();
        }

        public async Task<ProjectTask?> GetTaskAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"TasksApi/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProjectTask>();
        }

        public async Task<List<ProjectTask>> GetUserTasksAsync(string userId)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"TasksApi/user/{userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ProjectTask>>() ?? new List<ProjectTask>();
        }

        public async Task<ProjectTask?> CreateTaskAsync(ProjectTask task)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("TasksApi", task);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProjectTask>();
        }

        public async Task UpdateTaskAsync(int id, ProjectTask task)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync($"TasksApi/{id}", task);
            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateTaskStatusAsync(int id, string status)
        {
            AddAuthorizationHeader();
            var content = CreateJsonContent(status);
            var response = await _httpClient.PatchAsync($"TasksApi/{id}/status", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteTaskAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"TasksApi/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
