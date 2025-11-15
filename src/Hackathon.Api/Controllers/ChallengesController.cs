using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.Models;
using Hackathon.Api.DTOs;
using Supabase;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChallengesController : ControllerBase
{
    private readonly Client _supabase;
    private readonly ILogger<ChallengesController> _logger;

    public ChallengesController(Client supabase, ILogger<ChallengesController> logger)
    {
        _supabase = supabase;
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
}
