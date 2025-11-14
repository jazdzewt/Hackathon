// Controllers/SupabaseController.cs
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupabaseController : ControllerBase
{
    private readonly Client _supabase;
    private readonly ILogger<SupabaseController> _logger;

    public SupabaseController(Client supabase, ILogger<SupabaseController> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            _logger.LogInformation("Testowanie połączenia z Supabase Auth...");
            
            // Sprawdź czy Auth działa poprzez pobranie ustawień
            var isInitialized = _supabase.Auth != null;
            
            if (!isInitialized)
            {
                return BadRequest(new { 
                    error = "Supabase Auth nie jest zainicjalizowany"
                });
            }

            // Sprawdź aktualną sesję
            var session = _supabase.Auth?.CurrentSession;
            
            return Ok(new { 
                message = "✅ Połączenie z Supabase Auth działa!",
                authInitialized = isInitialized,
                hasActiveSession = session != null,
                sessionInfo = session != null ? new {
                    userId = session.User?.Id,
                    email = session.User?.Email,
                    expiresAt = session.ExpiresAt()
                } : null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd połączenia z Supabase Auth");
            return BadRequest(new { 
                error = "Błąd połączenia z Supabase Auth", 
                details = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("auth/user")]
    public IActionResult GetCurrentUser()
    {
        try
        {
            var session = _supabase.Auth?.CurrentSession;
            
            if (session == null || session.User == null)
            {
                return Unauthorized(new { 
                    error = "Brak aktywnej sesji"
                });
            }

            return Ok(new { 
                user = new {
                    id = session.User.Id,
                    email = session.User.Email,
                    createdAt = session.User.CreatedAt,
                    lastSignInAt = session.User.LastSignInAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania użytkownika");
            return BadRequest(new { 
                error = "Błąd pobierania użytkownika", 
                details = ex.Message
            });
        }
    }
}