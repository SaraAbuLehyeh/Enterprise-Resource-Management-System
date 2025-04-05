using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ERMS.Services
{
    public class ApiService
    {
        protected readonly HttpClient _httpClient;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly string _baseUrl;

        public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7080/api/";
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        protected void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JWTToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        protected StringContent CreateJsonContent(object data)
        {
            var json = JsonSerializer.Serialize(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}
