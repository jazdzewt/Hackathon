using Hackathon.Api.DTOs.Admin;
using Hackathon.Api.Models;
using Supabase;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hackathon.Api.Services;

public class AdminService : IAdminService
{
    private readonly Client _supabaseClient;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AdminService(Client supabaseClient, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _supabaseClient = supabaseClient;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(serviceRoleKey) || serviceRoleKey == "YOUR_SERVICE_ROLE_KEY_HERE")
            {
                throw new InvalidOperationException("Supabase ServiceRoleKey is not configured. Please add it to appsettings.json");
            }

            // Wywołaj Supabase Auth Admin API
            var request = new HttpRequestMessage(HttpMethod.Get, $"{supabaseUrl}/auth/v1/admin/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
            request.Headers.Add("apikey", serviceRoleKey);

            var response = await _httpClient.SendAsync(request);
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[ADMIN] Supabase Auth API response: {jsonResponse}");
            
            response.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var usersResponse = JsonSerializer.Deserialize<SupabaseUsersResponse>(jsonResponse, options);
            Console.WriteLine($"[ADMIN] Deserialized users count: {usersResponse?.Users?.Count ?? 0}");

            if (usersResponse?.Users == null || !usersResponse.Users.Any())
            {
                return Enumerable.Empty<UserDto>();
            }

            return usersResponse.Users.Select(user => new UserDto(
                Id: user.Id,
                Email: user.Email ?? "N/A",
                Username: user.UserMetadata?.TryGetValue("username", out var username) == true 
                    ? username?.ToString() ?? user.Email ?? "N/A"
                    : user.Email ?? "N/A",
                Role: user.UserMetadata?.TryGetValue("role", out var role) == true 
                    ? role?.ToString() ?? "participant" 
                    : "participant",
                CreatedAt: user.CreatedAt,
                IsActive: user.Banned == false
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching users from Supabase Auth: {ex.Message}");
            throw new Exception($"Error fetching users from Supabase Auth: {ex.Message}", ex);
        }
    }

    public async Task AssignRoleAsync(string userId, string roleName)
    {
        try
        {
            // Walidacja roli
            var validRoles = new[] { "participant", "admin", "judge" };
            if (!validRoles.Contains(roleName.ToLower()))
            {
                throw new ArgumentException($"Invalid role. Valid roles are: {string.Join(", ", validRoles)}");
            }

            var supabaseUrl = _configuration["Supabase:Url"];
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(serviceRoleKey))
            {
                throw new InvalidOperationException("Supabase ServiceRoleKey is not configured");
            }

            // Zaktualizuj user_metadata (role)
            var updatePayload = new
            {
                user_metadata = new Dictionary<string, object>
                {
                    { "role", roleName.ToLower() }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Put, $"{supabaseUrl}/auth/v1/admin/users/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
            request.Headers.Add("apikey", serviceRoleKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(updatePayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }
                throw new Exception($"Failed to update user role: {errorContent}");
            }
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not KeyNotFoundException && ex is not InvalidOperationException)
        {
            throw new Exception($"Error assigning role to user: {ex.Message}", ex);
        }
    }

    public async Task DeleteUserAsync(string userId)
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(serviceRoleKey))
            {
                throw new InvalidOperationException("Supabase ServiceRoleKey is not configured");
            }

            // Usuń użytkownika przez Supabase Auth Admin API
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{supabaseUrl}/auth/v1/admin/users/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
            request.Headers.Add("apikey", serviceRoleKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new Exception($"Error deleting user: {ex.Message}", ex);
        }
    }
}

// Model odpowiedzi z Supabase Auth API
internal class SupabaseUsersResponse
{
    [JsonPropertyName("users")]
    public List<SupabaseAuthUser> Users { get; set; } = new();
}

internal class SupabaseAuthUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    
    [JsonPropertyName("user_metadata")]
    public Dictionary<string, object>? UserMetadata { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("banned")]
    public bool Banned { get; set; }
}
