namespace Hackathon.Api.DTOs.Auth;

public record ResetPasswordDto(
    string Token,
    string NewPassword,
    string ConfirmPassword
);
