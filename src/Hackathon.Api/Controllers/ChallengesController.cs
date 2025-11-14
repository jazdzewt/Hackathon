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

    /// <summary>
    /// Tworzy nowe wyzwanie (TYLKO ADMIN)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChallengeResponse>> Create([FromBody] CreateChallengeRequest request)
    {
        try
        {
            _logger.LogInformation($"Tworzenie nowego wyzwania: {request.Title}");

            // Sprawdź czy użytkownik jest zalogowany
            var session = _supabase.Auth.CurrentSession;
            if (session?.User == null)
            {
                return Unauthorized(new { error = "Musisz być zalogowany" });
            }

            // TODO: Sprawdź czy użytkownik ma rolę admin
            // Można dodać sprawdzenie roli z tabeli users

            var challenge = new Challenge
            {
                Id = Guid.NewGuid().ToString(),
                Title = request.Title,
                Description = request.Description,
                EvaluationMetric = request.EvaluationMetric,
                SubmissionDeadline = request.SubmissionDeadline,
                DatasetUrl = request.DatasetUrl,
                MaxFileSizeMb = request.MaxFileSizeMb,
                AllowedFileTypes = request.AllowedFileTypes,
                IsActive = true,
                CreatedBy = session.User.Id,
                CreatedAt = DateTime.UtcNow
            };

            var response = await _supabase
                .From<Challenge>()
                .Insert(challenge);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                return BadRequest(new { error = "Nie udało się utworzyć wyzwania" });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                new ChallengeResponse(
                    created.Id,
                    created.Title,
                    created.Description ?? "",
                    created.EvaluationMetric,
                    created.SubmissionDeadline,
                    created.IsActive,
                    created.DatasetUrl,
                    created.MaxFileSizeMb,
                    created.AllowedFileTypes
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd tworzenia wyzwania");
            return BadRequest(new { error = "Błąd tworzenia wyzwania", details = ex.Message });
        }
    }

    /// <summary>
    /// Aktualizuje wyzwanie (TYLKO ADMIN)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ChallengeResponse>> Update(string id, [FromBody] UpdateChallengeRequest request)
    {
        try
        {
            _logger.LogInformation($"Aktualizacja wyzwania: {id}");

            // Sprawdź czy użytkownik jest zalogowany
            var session = _supabase.Auth.CurrentSession;
            if (session?.User == null)
            {
                return Unauthorized(new { error = "Musisz być zalogowany" });
            }

            // Pobierz istniejące wyzwanie
            var existing = await _supabase
                .From<Challenge>()
                .Where(c => c.Id == id)
                .Single();

            if (existing == null)
            {
                return NotFound(new { error = "Wyzwanie nie znalezione" });
            }

            // Aktualizuj tylko podane pola
            if (request.Title != null) existing.Title = request.Title;
            if (request.Description != null) existing.Description = request.Description;
            if (request.EvaluationMetric != null) existing.EvaluationMetric = request.EvaluationMetric;
            if (request.SubmissionDeadline.HasValue) existing.SubmissionDeadline = request.SubmissionDeadline.Value;
            if (request.DatasetUrl != null) existing.DatasetUrl = request.DatasetUrl;
            if (request.IsActive.HasValue) existing.IsActive = request.IsActive.Value;

            var response = await _supabase
                .From<Challenge>()
                .Where(c => c.Id == id)
                .Update(existing);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                return BadRequest(new { error = "Nie udało się zaktualizować wyzwania" });
            }

            return Ok(new ChallengeResponse(
                updated.Id,
                updated.Title,
                updated.Description ?? "",
                updated.EvaluationMetric,
                updated.SubmissionDeadline,
                updated.IsActive,
                updated.DatasetUrl,
                updated.MaxFileSizeMb,
                updated.AllowedFileTypes
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd aktualizacji wyzwania");
            return BadRequest(new { error = "Błąd aktualizacji wyzwania", details = ex.Message });
        }
    }
}
