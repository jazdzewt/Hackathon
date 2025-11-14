using Hackathon.Api.DTOs.Auth;

namespace Hackathon.Api.Services;

public interface IAuthService
{
    Task<TokenResponseDto> RegisterAsync(RegisterUserDto dto);
    Task<TokenResponseDto> LoginAsync(LoginDto dto);
    Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto);
    Task SendPasswordResetLinkAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}
