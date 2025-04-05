using ERMS.Models;
using System.Net.Http.Json;

namespace ERMS.Services
{
    public class EmployeeApiService : ApiService
    {
        public EmployeeApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
            : base(httpClient, httpContextAccessor, configuration)
        {
        }

        public async Task<List<User>> GetEmployeesAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync("EmployeesApi");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<User>>() ?? new List<User>();
        }

        public async Task<User?> GetEmployeeAsync(string id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"EmployeesApi/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>();
        }

        public async Task<User?> CreateEmployeeAsync(User employee)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync("EmployeesApi", employee);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>();
        }

        public async Task UpdateEmployeeAsync(string id, User employee)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync($"EmployeesApi/{id}", employee);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteEmployeeAsync(string id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"EmployeesApi/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
