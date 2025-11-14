namespace Hackathon.Api.DTOs.Auth;

public record RegisterUserDto(
    string Email,
    string Password,
    string ConfirmPassword,
    string Username
);
