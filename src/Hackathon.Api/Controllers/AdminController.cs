using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.DTOs.Admin;
using Hackathon.Api.DTOs.Challenges;
using Hackathon.Api.DTOs.Submissions;
using Hackathon.Api.Services;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
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
        // TODO: Implementacja pobierania listy użytkowników
        return Ok(new List<UserDto>());
    }

    /// <summary>
    /// Przypisuje rolę użytkownikowi
    /// </summary>
    [HttpPost("users/{userId}/assign-role")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleDto dto)
    {
        // TODO: Implementacja przypisywania roli
        return Ok(new { message = "Role assigned successfully" });
    }

    /// <summary>
    /// Banuje lub usuwa użytkownika
    /// </summary>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        // TODO: Implementacja usuwania użytkownika
        return Ok(new { message = "User deleted successfully" });
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
