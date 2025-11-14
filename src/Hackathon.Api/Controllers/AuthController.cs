using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.DTOs.Auth;
using Hackathon.Api.Services;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Rejestruje nowego użytkownika
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        // TODO: Implementacja rejestracji
        return Ok(new { message = "User registered successfully" });
    }

    /// <summary>
    /// Loguje użytkownika i zwraca token JWT
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto dto)
    {
        // TODO: Implementacja logowania
        return Ok(new TokenResponseDto("access_token", "refresh_token", DateTime.UtcNow.AddHours(1)));
    }

    /// <summary>
    /// Odświeża wygasły token JWT
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshTokenDto dto)
    {
        // TODO: Implementacja odświeżania tokenu
        return Ok(new TokenResponseDto("new_access_token", "new_refresh_token", DateTime.UtcNow.AddHours(1)));
    }

    /// <summary>
    /// Wysyła link do resetowania hasła
    /// </summary>
    [HttpPost("forgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        // TODO: Implementacja wysyłania linku resetującego
        return Ok(new { message = "Password reset link sent" });
    }

    /// <summary>
    /// Resetuje hasło użytkownika
    /// </summary>
    [HttpPost("resetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        // TODO: Implementacja resetowania hasła
        return Ok(new { message = "Password reset successfully" });
    }
}
