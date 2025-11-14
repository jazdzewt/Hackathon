using Hackathon.Api.DTOs.Auth;
using Hackathon.Api.Models;
using Supabase;
using System.Security.Cryptography;
using System.Text;

namespace Hackathon.Api.Services;

public class AuthService : IAuthService
{
    private readonly Supabase.Client _supabaseClient;

    public AuthService(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        try
        {
            // Walidacja
            if (dto.Password != dto.ConfirmPassword)
            {
                throw new ArgumentException("Passwords do not match");
            }

            // Rejestracja przez Supabase Auth
            var session = await _supabaseClient.Auth.SignUp(dto.Email, dto.Password);
            
            if (session?.User == null)
            {
                throw new Exception("Registration failed");
            }

            // Dodaj profil użytkownika do tabeli profiles z domyślną rolą 'user'
            var profile = new Profile
            {
                Uid = session.User?.Id ?? string.Empty,
                Role = "user", // domyślnie wszyscy są 'user', admin ustawia się ręcznie w bazie
                CreatedAt = DateTime.UtcNow
            };

            await _supabaseClient
                .From<Profile>()
                .Insert(profile);

            return new TokenResponseDto(
                AccessToken: session.AccessToken ?? string.Empty,
                RefreshToken: session.RefreshToken ?? string.Empty,
                ExpiresAt: DateTime.UtcNow.AddHours(1)
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Registration failed: {ex.Message}", ex);
        }
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        try
        {
            var session = await _supabaseClient.Auth.SignIn(dto.Email, dto.Password);

            if (session?.User == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            return new TokenResponseDto(
                AccessToken: session.AccessToken ?? string.Empty,
                RefreshToken: session.RefreshToken ?? string.Empty,
                ExpiresAt: DateTime.UtcNow.AddHours(1)
            );
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException($"Login failed: {ex.Message}");
        }
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
    {
        try
        {
            var session = await _supabaseClient.Auth.RefreshSession();

            if (session == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            return new TokenResponseDto(
                AccessToken: session.AccessToken ?? string.Empty,
                RefreshToken: session.RefreshToken ?? string.Empty,
                ExpiresAt: DateTime.UtcNow.AddHours(1)
            );
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task SendPasswordResetLinkAsync(ForgotPasswordDto dto)
    {
        try
        {
            await _supabaseClient.Auth.ResetPasswordForEmail(dto.Email);
        }
        catch (Exception ex)
        {
            // Nie rzucaj błędu jeśli email nie istnieje (security best practice)
            // Log the error but return success to user
            Console.WriteLine($"Password reset error: {ex.Message}");
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new ArgumentException("Passwords do not match");
            }

            // Supabase obsługuje reset hasła przez email link
            // Token jest weryfikowany automatycznie przez Supabase
            var attributes = new Supabase.Gotrue.UserAttributes
            {
                Password = dto.NewPassword
            };
            
            await _supabaseClient.Auth.Update(attributes);
        }
        catch (Exception ex)
        {
            throw new Exception($"Password reset failed: {ex.Message}", ex);
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _supabaseClient.Auth.SignOut();
        }
        catch (Exception ex)
        {
            throw new Exception($"Logout failed: {ex.Message}", ex);
        }
    }
}
