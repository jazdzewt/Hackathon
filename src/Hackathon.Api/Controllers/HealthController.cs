using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly Client _supabase;
    private readonly ILogger<HealthController> _logger;

    public HealthController(Client supabase, ILogger<HealthController> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "✅ API działa!",
            service = "Hackathon.Api",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("supabase")]
    public IActionResult GetSupabaseHealth()
    {
        try
        {
            // Sprawdź czy Auth jest zainicjalizowany
            var isAuthInitialized = _supabase.Auth != null;
            var session = _supabase.Auth?.CurrentSession;

            return Ok(new
            {
                status = "✅ Połączenie z Supabase działa!",
                authInitialized = isAuthInitialized,
                hasActiveSession = session != null,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd sprawdzania Supabase");
            return Ok(new
            {
                status = "❌ Błąd połączenia z Supabase",
                error = ex.Message,
                details = ex.InnerException?.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
