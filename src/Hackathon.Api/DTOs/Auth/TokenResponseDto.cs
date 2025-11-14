namespace Hackathon.Api.DTOs.Auth;

public record TokenResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);
