namespace Hackathon.Api.DTOs.Admin;

public record UserDto(
    string Id,
    string Email,
    string Username,
    string Role,
    DateTime CreatedAt,
    bool IsActive
);
