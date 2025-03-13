using Microsoft.AspNetCore.SignalR.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

namespace ToxiqChatTester
{
    public class AuthenticationHelper
    {
        private string _baseUrl;
        private string _jwtToken;
        private HttpClient _httpClient;

        public AuthenticationHelper(string baseUrl, string jwtToken)
        {
            _baseUrl = baseUrl;
            _jwtToken = jwtToken;

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);

            if (!string.IsNullOrEmpty(_jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
            }
        }

        public string GetUserIdFromToken()
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(_jwtToken) as JwtSecurityToken;

                if (jsonToken == null)
                {
                    return null;
                }

                // Look for nameid or nameidentifier claim which typically contains the user ID
                var userIdClaim = jsonToken.Claims.FirstOrDefault(c =>
                    c.Type == "nameid" ||
                    c.Type == "nameidentifier" ||
                    c.Type == "sub" ||
                    c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                return userIdClaim?.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ValidateToken()
        {
            try
            {
                // Try to use a lightweight API endpoint that requires authentication
                var response = await _httpClient.GetAsync("api/User/GetMe");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<HubConnection> CreateHubConnection(string hubUrl)
        {
            var connection = new HubConnectionBuilder()
       .WithUrl($"{_baseUrl}/hubs/chat", options =>
       {
           // Just the token without "Bearer" prefix
           options.AccessTokenProvider = () => Task.FromResult(_jwtToken);

           // Keep the Authorization header with Bearer prefix for the initial HTTP request
           options.Headers.Add("Authorization", $"Bearer {_jwtToken}");
       })
       .WithAutomaticReconnect()
       .Build();

            return connection;
        }


        public async Task<HubConnection> TryConnectToHub(string hubUrl, Action<string> statusCallback)
        {
            try
            {
                statusCallback?.Invoke($"Creating connection to {hubUrl}...");
                var connection = await CreateHubConnection(hubUrl);

                statusCallback?.Invoke($"Starting connection to {hubUrl}...");
                await connection.StartAsync();

                statusCallback?.Invoke($"Connected to {hubUrl}");
                return connection;
            }
            catch (Exception ex)
            {
                statusCallback?.Invoke($"Failed to connect to {hubUrl}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    statusCallback?.Invoke($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }
    }
}