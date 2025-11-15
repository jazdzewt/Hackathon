using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Hackathon.Api.DTOs.Admin;
using Hackathon.Api.DTOs.Challenges;
using Hackathon.Api.DTOs.Submissions;
using Hackathon.Api.Services;
using Supabase;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
[EnableRateLimiting("admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IChallengeService _challengeService;
    private readonly ISubmissionService _submissionService;
    private readonly IScoringService _scoringService;
    private readonly ILogger<AdminController> _logger;
    private readonly Client _supabaseClient;

    public AdminController(
        IAdminService adminService,
        IChallengeService challengeService,
        ISubmissionService submissionService,
        IScoringService scoringService,
        ILogger<AdminController> logger,
        Client supabaseClient)
    {
        _adminService = adminService;
        _challengeService = challengeService;
        _submissionService = submissionService;
        _scoringService = scoringService;
        _logger = logger;
        _supabaseClient = supabaseClient;
    }

    #region Challenge Management

    /// <summary>
    /// Tworzy nowe wyzwanie (bez datasetu)
    /// </summary>
    [HttpPost("challenges")]
    public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeDto dto)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            var challenge = new Models.Challenge
            {
                // NIE ustawiamy Id - baza wygeneruje UUID automatycznie
                Title = dto.Name,
                Description = dto.FullDescription,
                EvaluationMetric = dto.EvaluationMetric,
                SubmissionDeadline = dto.EndDate ?? dto.StartDate.AddMonths(1),
                MaxFileSizeMb = dto.MaxFileSizeMb ?? 100,
                AllowedFileTypes = dto.AllowedFileTypes,
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _challengeService.CreateChallengeAsync(challenge);
            
            return Ok(new { message = "Challenge created successfully", id = challenge.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error creating challenge", error = ex.Message });
        }
    }

    /// <summary>
    /// Tworzy nowe wyzwanie z datasetem
    /// </summary>
    [HttpPost("challenges/with-dataset")]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)] // Ukryj w Swaggerze - użyj Postmana
    public async Task<IActionResult> CreateChallengeWithDataset(
        [FromForm] string name,
        [FromForm] string shortDescription,
        [FromForm] string fullDescription,
        [FromForm] string rules,
        [FromForm] string evaluationMetric,
        [FromForm] DateTime startDate,
        [FromForm] DateTime? endDate,
        [FromForm] int? maxFileSizeMb,
        [FromForm] string? allowedFileTypes, // comma-separated: "csv,json,txt"
        [FromForm] IFormFile? datasetFile)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            var challenge = new Models.Challenge
            {
                // NIE ustawiamy Id - baza wygeneruje UUID automatycznie
                Title = name,
                Description = fullDescription,
                EvaluationMetric = evaluationMetric,
                SubmissionDeadline = endDate ?? startDate.AddMonths(1),
                MaxFileSizeMb = maxFileSizeMb ?? 100,
                AllowedFileTypes = string.IsNullOrEmpty(allowedFileTypes) 
                    ? null 
                    : allowedFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Jeśli jest dataset, upload go do storage
            if (datasetFile != null && datasetFile.Length > 0)
            {
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await datasetFile.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                var datasetUrl = await _challengeService.CreateChallengeWithDatasetAsync(
                    challenge, 
                    fileBytes, 
                    datasetFile.FileName);

                return Ok(new 
                { 
                    message = "Challenge created successfully with dataset", 
                    id = challenge.Id,
                    datasetUrl = datasetUrl
                });
            }
            else
            {
                await _challengeService.CreateChallengeAsync(challenge);
                return Ok(new { message = "Challenge created successfully (no dataset)", id = challenge.Id });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error creating challenge", error = ex.Message });
        }
    }

    /// <summary>
    /// Edytuje istniejące wyzwanie
    /// </summary>
    [HttpPut("challenges/{id}")]
    public async Task<IActionResult> UpdateChallenge(string id, [FromBody] UpdateChallengeDto dto)
    {
        try
        {
            await _challengeService.UpdateChallengeAsync(id, dto);
            return Ok(new { message = "Challenge updated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error updating challenge", error = ex.Message });
        }
    }

    /// <summary>
    /// Usuwa wyzwanie
    /// </summary>
    [HttpDelete("challenges/{id}")]
    public async Task<IActionResult> DeleteChallenge(string id)
    {
        try
        {
            await _challengeService.DeleteChallengeAsync(id);
            return Ok(new { message = "Challenge deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error deleting challenge", error = ex.Message });
        }
    }

    /// <summary>
    /// Przesyła plik z poprawnymi odpowiedziami (ground truth) - TYLKO ADMIN
    /// </summary>
    [HttpPost("challenges/{id}/ground-truth")]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)] // Ukryj w Swaggerze - użyj Postmana
    public async Task<IActionResult> UploadGroundTruth(string id, [FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "File is required" });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Upload ground-truth do Storage (przez ChallengeService)
            // Funkcja automatycznie zapisuje URL w challenges.ground_truth_url
            var groundTruthUrl = await _challengeService.UploadGroundTruthAsync(id, fileBytes, file.FileName);

            _logger.LogInformation($"Ground truth uploaded for challenge {id} by admin {userId}");

            return Ok(new 
            { 
                message = "Ground truth file uploaded successfully (hidden from participants)", 
                groundTruthUrl = groundTruthUrl 
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading ground truth for challenge {id}");
            return StatusCode(500, new { error = "Error uploading ground truth", details = ex.Message });
        }
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
    /// Ręcznie ocenia submission (Admin/Judge)
    /// </summary>
    [HttpPost("submissions/{submissionId}/score")]
    public async Task<IActionResult> ManuallyScoreSubmission(string submissionId, [FromBody] DTOs.Submissions.ManualScoreDto dto)
    {
        try
        {
            var evaluatorId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(evaluatorId))
            {
                return Unauthorized(new { error = "Evaluator not authenticated" });
            }

            await _scoringService.ManuallyScoreSubmissionAsync(submissionId, dto.Score, dto.Notes, evaluatorId);

            _logger.LogInformation($"Submission {submissionId} manually scored by {evaluatorId} with score: {dto.Score}");

            return Ok(new 
            { 
                message = "Submission scored successfully", 
                score = dto.Score,
                evaluatedBy = evaluatorId
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error manually scoring submission {submissionId}");
            return StatusCode(500, new { error = "Error scoring submission", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera wszystkie zgłoszenia dla wyzwania
    /// </summary>
    [HttpGet("submissions/challenges/{challengeId}")]
    public async Task<ActionResult<IEnumerable<SubmissionDto>>> GetChallengeSubmissions(string challengeId)
    {
        try
        {
            var submissions = await _submissionService.GetChallengeSubmissionsAsync(challengeId);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching submissions for challenge {challengeId}");
            return StatusCode(500, new { error = "Error fetching submissions", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera plik submission (tylko admin)
    /// </summary>
    [HttpGet("submissions/{submissionId}/download")]
    public async Task<IActionResult> DownloadSubmission(string submissionId)
    {
        try
        {
            var submission = await _supabaseClient
                .From<Models.Submission>()
                .Where(s => s.Id == submissionId)
                .Single();

            if (submission == null)
            {
                return NotFound(new { error = "Submission not found" });
            }

            // Pobierz plik z URL
            using var httpClient = new HttpClient();
            var fileBytes = await httpClient.GetByteArrayAsync(submission.FileUrl);

            var contentType = GetContentType(Path.GetExtension(submission.FileName));
            return File(fileBytes, contentType, submission.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading submission {submissionId}");
            return StatusCode(500, new { error = "Error downloading submission", details = ex.Message });
        }
    }

    /// <summary>
    /// Wymusza ponowne automatyczne przeliczenie wyniku zgłoszenia
    /// </summary>
    [HttpPost("submissions/{submissionId}/reevaluate")]
    public async Task<IActionResult> ReevaluateSubmission(string submissionId)
    {
        try
        {
            await _submissionService.EvaluateSubmissionAsync(submissionId);
            
            _logger.LogInformation($"Submission {submissionId} queued for re-evaluation");

            return Ok(new { message = "Submission queued for re-evaluation" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error re-evaluating submission {submissionId}");
            return StatusCode(500, new { error = "Error re-evaluating submission", details = ex.Message });
        }
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

    #region Helper Methods

    private static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    #endregion
}
