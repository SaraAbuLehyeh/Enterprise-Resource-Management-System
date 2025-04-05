using System.Net.Http.Json;

namespace ERMS.Services
{
    public class AuthApiService : ApiService
    {
        public AuthApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
            : base(httpClient, httpContextAccessor, configuration)
        {
        }

        public async Task<string?> GetTokenAsync(string username, string password)
        {
            var loginModel = new LoginModel
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("Auth/token", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
                return tokenResponse?.Token;
            }

            return null;
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}
