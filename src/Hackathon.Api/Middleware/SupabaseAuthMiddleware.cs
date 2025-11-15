using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hackathon.Api.Models;

namespace Hackathon.Api.Middleware;

/// <summary>
/// Middleware weryfikujący JWT z Supabase i dodający rolę z tabeli profiles
/// </summary>
public class SupabaseAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SupabaseAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, Supabase.Client supabaseClient)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        var token = authHeader?.Split(" ").Last();
        
        Console.WriteLine($"[AUTH DEBUG] Path: {context.Request.Path}, Has token: {!string.IsNullOrEmpty(token)}");

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Zdekoduj JWT z Supabase
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Pobierz user_id i email z tokena
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                
                Console.WriteLine($"[AUTH DEBUG] UserId: {userId}");

                if (!string.IsNullOrEmpty(userId))
                {
                    // Pobierz rolę z tabeli profiles
                    var profileResponse = await supabaseClient
                        .From<Profile>()
                        .Filter("uid", Supabase.Postgrest.Constants.Operator.Equals, userId)
                        .Get();

                    var profile = profileResponse?.Models?.FirstOrDefault();
                    var role = profile?.Role ?? "user";
                    
                    Console.WriteLine($"[AUTH DEBUG] Role: {role}");

                    // ⚡ DODAJ userId DO HttpContext.Items (zamiast Session)
                    context.Items["UserId"] = userId;
                    context.Items["UserEmail"] = email;

                    // Dodaj claims do kontekstu
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Email, email ?? string.Empty),
                        new Claim(ClaimTypes.Role, role)
                    };

                    var identity = new ClaimsIdentity(claims, "SupabaseAuth");
                    context.User = new ClaimsPrincipal(identity);
                    
                    Console.WriteLine($"[AUTH DEBUG] User authenticated: {context.User.Identity?.IsAuthenticated}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH ERROR] {ex.Message}");
            }
        }

        await _next(context);
    }
}
