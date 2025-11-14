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

    [HttpPost("auth/signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        try
        {
            _logger.LogInformation($"Rejestracja użytkownika: {request.Email}");
            
            var response = await _supabase.Auth.SignUp(request.Email, request.Password);
            
            if (response?.User == null)
            {
                return BadRequest(new { 
                    error = "Nie udało się zarejestrować użytkownika"
                });
            }

            return Ok(new { 
                message = "✅ Użytkownik zarejestrowany!",
                user = new {
                    id = response.User.Id,
                    email = response.User.Email,
                    createdAt = response.User.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd rejestracji");
            return BadRequest(new { 
                error = "Błąd rejestracji", 
                details = ex.Message
            });
        }
    }

    [HttpPost("auth/signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        try
        {
            _logger.LogInformation($"Logowanie użytkownika: {request.Email}");
            
            var response = await _supabase.Auth.SignIn(request.Email, request.Password);
            
            if (response?.User == null)
            {
                return Unauthorized(new { 
                    error = "Nieprawidłowy email lub hasło"
                });
            }

            return Ok(new { 
                message = "✅ Zalogowano pomyślnie!",
                user = new {
                    id = response.User.Id,
                    email = response.User.Email
                },
                session = new {
                    accessToken = response.AccessToken,
                    refreshToken = response.RefreshToken,
                    expiresIn = response.ExpiresIn
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd logowania");
            return BadRequest(new { 
                error = "Błąd logowania", 
                details = ex.Message
            });
        }
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            _logger.LogInformation("Wylogowywanie użytkownika");
            
            await _supabase.Auth.SignOut();
            
            return Ok(new { 
                message = "✅ Wylogowano pomyślnie!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wylogowania");
            return BadRequest(new { 
                error = "Błąd wylogowania", 
                details = ex.Message
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

// Request models
public record SignUpRequest(string Email, string Password);
public record SignInRequest(string Email, string Password);