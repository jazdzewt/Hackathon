using Hackathon.Api.DTOs.Admin;

namespace Hackathon.Api.Services;

public interface IAdminService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task AssignRoleAsync(string userId, string roleName);
    Task DeleteUserAsync(string userId);
}
