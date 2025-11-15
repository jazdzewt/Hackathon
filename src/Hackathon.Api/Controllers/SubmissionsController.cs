using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Hackathon.Api.Models;
using Hackathon.Api.DTOs;
using Hackathon.Api.Services;
using System.Security.Claims;
using Supabase;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly ILogger<SubmissionsController> _logger;
    private readonly Client _supabaseClient;

    public SubmissionsController(ISubmissionService submissionService, ILogger<SubmissionsController> logger, Client supabaseClient)
    {
        _submissionService = submissionService;
        _logger = logger;
        _supabaseClient = supabaseClient;
    }

    /// <summary>
    /// Przesyła rozwiązanie dla wyzwania (z automatycznym asynchronicznym ocenianiem)
    /// </summary>
    [HttpPost("challenges/{challengeId}/submit")]
    [Authorize]
    [EnableRateLimiting("submissions")]
    [Consumes("multipart/form-data")]
    [ApiExplorerSettings(IgnoreApi = true)] // Ukryj w Swaggerze - użyj Postmana
    public async Task<IActionResult> Submit(string challengeId, [FromForm] IFormFile file)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            _logger.LogInformation($"Submission from user {userId} for challenge {challengeId}");

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "File is required" });
            }

            var submissionId = await _submissionService.SubmitSolutionAsync(challengeId, userId, file);

            return Ok(new
            {
                message = "Submission accepted and will be evaluated shortly",
                submissionId = submissionId
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting solution");
            return StatusCode(500, new { error = "Submission failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera historię zgłoszeń zalogowanego użytkownika
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMySubmissions()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var submissions = await _submissionService.GetUserSubmissionsAsync(userId);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user submissions");
            return StatusCode(500, new { error = "Failed to fetch submissions", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera zgłoszenia dla konkretnego wyzwania
    /// </summary>
    [HttpGet("challenges/{challengeId}")]
    public async Task<IActionResult> GetByChallengeId(string challengeId)
    {
        try
        {
            var submissions = await _submissionService.GetChallengeSubmissionsAsync(challengeId);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching challenge submissions");
            return StatusCode(500, new { error = "Failed to fetch submissions", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera plik submission
    /// </summary>
    [HttpGet("{submissionId}/download")]
    [Authorize]
    public async Task<IActionResult> DownloadSubmission(string submissionId)
    {
        try
        {
            var submission = await _supabaseClient
                .From<Submission>()
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
}
