using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hackathon.Api.Models;
using Hackathon.Api.DTOs;
using Hackathon.Api.DTOs.Challenges;
using Hackathon.Api.Services;
using Supabase;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChallengesController : ControllerBase
{
    private readonly Client _supabase;
    private readonly IChallengeService _challengeService;
    private readonly ILogger<ChallengesController> _logger;

    public ChallengesController(Client supabase, IChallengeService challengeService, ILogger<ChallengesController> logger)
    {
        _supabase = supabase;
        _challengeService = challengeService;
        _logger = logger;
    }

    /// <summary>
    /// Pobiera listę wszystkich aktywnych wyzwań
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChallengeResponse>>> GetAll()
    {
        try
        {
            _logger.LogInformation("Pobieranie listy wyzwań");
            
            var response = await _supabase
                .From<Challenge>()
                .Where(c => c.IsActive == true)
                .Get();

            var challenges = response.Models.Select(c => new ChallengeResponse(
                c.Id,
                c.Title,
                c.Description ?? "",
                c.EvaluationMetric,
                c.SubmissionDeadline,
                c.IsActive,
                c.DatasetUrl,
                c.MaxFileSizeMb,
                c.AllowedFileTypes
            ));

            return Ok(challenges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania wyzwań");
            return BadRequest(new { error = "Błąd pobierania wyzwań", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera szczegóły konkretnego wyzwania
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ChallengeResponse>> GetById(string id)
    {
        try
        {
            _logger.LogInformation($"Pobieranie wyzwania: {id}");
            
            var response = await _supabase
                .From<Challenge>()
                .Where(c => c.Id == id)
                .Single();

            if (response == null)
            {
                return NotFound(new { error = "Wyzwanie nie znalezione" });
            }

            return Ok(new ChallengeResponse(
                response.Id,
                response.Title,
                response.Description ?? "",
                response.EvaluationMetric,
                response.SubmissionDeadline,
                response.IsActive,
                response.DatasetUrl,
                response.MaxFileSizeMb,
                response.AllowedFileTypes
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania wyzwania");
            return BadRequest(new { error = "Błąd pobierania wyzwania", details = ex.Message });
        }
    }

    /// <summary>
    /// Pobiera dataset dla wyzwania (plik z danymi treningowymi)
    /// </summary>
    [HttpGet("{id}/dataset")]
    public async Task<IActionResult> DownloadDataset(string id)
    {
        try
        {
            var challenge = await _supabase
                .From<Challenge>()
                .Where(c => c.Id == id)
                .Single();

            if (challenge == null)
            {
                return NotFound(new { error = "Challenge not found" });
            }

            if (string.IsNullOrEmpty(challenge.DatasetUrl))
            {
                return NotFound(new { error = "Dataset not available for this challenge" });
            }

            // Pobierz plik z URL
            using var httpClient = new HttpClient();
            var fileBytes = await httpClient.GetByteArrayAsync(challenge.DatasetUrl);

            // Wyciągnij nazwę pliku z URL
            var fileName = Path.GetFileName(new Uri(challenge.DatasetUrl).LocalPath);
            var contentType = GetContentType(Path.GetExtension(fileName));

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading dataset for challenge {id}");
            return StatusCode(500, new { error = "Error downloading dataset", details = ex.Message });
        }
    }

    /// <summary>
    /// Aktualizuje wyzwanie (wymaga autoryzacji)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
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
    /// Usuwa wyzwanie (wymaga autoryzacji)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
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

    private static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            ".parquet" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }
}
