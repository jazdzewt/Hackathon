using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.DTOs.Admin;
using Hackathon.Api.DTOs.Challenges;
using Hackathon.Api.DTOs.Submissions;
using Hackathon.Api.Services;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IChallengeService _challengeService;
    private readonly ISubmissionService _submissionService;

    public AdminController(
        IAdminService adminService,
        IChallengeService challengeService,
        ISubmissionService submissionService)
    {
        _adminService = adminService;
        _challengeService = challengeService;
        _submissionService = submissionService;
    }

    #region Challenge Management

    /// <summary>
    /// Tworzy nowe wyzwanie
    /// </summary>
    [HttpPost("challenges")]
    public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeDto dto)
    {
        // TODO: Implementacja tworzenia wyzwania
        return Ok(new { message = "Challenge created successfully" });
    }

    /// <summary>
    /// Edytuje istniejące wyzwanie
    /// </summary>
    [HttpPut("challenges/{id}")]
    public async Task<IActionResult> UpdateChallenge(int id, [FromBody] UpdateChallengeDto dto)
    {
        // TODO: Implementacja edycji wyzwania
        return Ok(new { message = "Challenge updated successfully" });
    }

    /// <summary>
    /// Usuwa wyzwanie
    /// </summary>
    [HttpDelete("challenges/{id}")]
    public async Task<IActionResult> DeleteChallenge(int id)
    {
        // TODO: Implementacja usuwania wyzwania
        return Ok(new { message = "Challenge deleted successfully" });
    }

    /// <summary>
    /// Przesyła plik z poprawnymi odpowiedziami (ground truth)
    /// </summary>
    [HttpPost("challenges/{id}/ground-truth")]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)] // Ukryj w Swaggerze - użyj Postmana
    public async Task<IActionResult> UploadGroundTruth(int id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required" });
        }

        // TODO: Implementacja przesyłania pliku ground truth
        return Ok(new { message = "Ground truth file uploaded successfully" });
    }

    #endregion

    #region User Management

    /// <summary>
    /// Pobiera listę wszystkich użytkowników
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        try
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error fetching users", error = ex.Message });
        }
    }

    /// <summary>
    /// Przypisuje rolę użytkownikowi
    /// </summary>
    [HttpPost("users/{userId}/assign-role")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleDto dto)
    {
        try
        {
            await _adminService.AssignRoleAsync(userId, dto.RoleName);
            return Ok(new { message = $"Role '{dto.RoleName}' assigned successfully to user {userId}" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error assigning role", error = ex.Message });
        }
    }

    /// <summary>
    /// Banuje lub usuwa użytkownika
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            await _adminService.DeleteUserAsync(userId);
            return Ok(new { message = $"User {userId} deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
        }
    }

    #endregion

    #region Submission and Leaderboard Management

    /// <summary>
    /// Pobiera wszystkie zgłoszenia dla wyzwania
    /// </summary>
    [HttpGet("submissions/{challengeId}")]
    public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetChallengeSubmissions(int challengeId)
    {
        // TODO: Implementacja pobierania zgłoszeń dla wyzwania
        return Ok(new List<SubmissionDto>());
    }

    /// <summary>
    /// Wymusza ponowne przeliczenie wyniku zgłoszenia
    /// </summary>
    [HttpPost("submissions/{submissionId}/rejudge")]
    public async Task<IActionResult> RejudgeSubmission(int submissionId)
    {
        // TODO: Implementacja ponownego oceniania
        return Ok(new { message = "Submission queued for rejudging" });
    }

    /// <summary>
    /// Zamraża publiczny ranking dla wyzwania
    /// </summary>
    [HttpPost("leaderboard/{challengeId}/freeze")]
    public async Task<IActionResult> FreezeLeaderboard(int challengeId)
    {
        // TODO: Implementacja zamrażania rankingu
        return Ok(new { message = "Leaderboard frozen successfully" });
    }

    #endregion
}
