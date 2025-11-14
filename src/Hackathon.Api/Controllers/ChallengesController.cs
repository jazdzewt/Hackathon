using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.DTOs.Challenges;
using Hackathon.Api.Services;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChallengesController : ControllerBase
{
    private readonly IChallengeService _challengeService;

    public ChallengesController(IChallengeService challengeService)
    {
        _challengeService = challengeService;
    }

    /// <summary>
    /// Pobiera listę wszystkich dostępnych wyzwań
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChallengeListDto>>> GetAll()
    {
        // TODO: Implementacja pobierania listy wyzwań
        return Ok(new List<ChallengeListDto>());
    }

    /// <summary>
    /// Pobiera szczegóły konkretnego wyzwania
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ChallengeDetailDto>> GetById(int id)
    {
        // TODO: Implementacja pobierania szczegółów wyzwania
        return Ok();
    }

    /// <summary>
    /// Pobiera plik z danymi treningowymi dla wyzwania
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDataset(int id)
    {
        // TODO: Implementacja pobierania pliku z danymi
        var fileBytes = Array.Empty<byte>();
        return File(fileBytes, "application/zip", $"challenge_{id}_data.zip");
    }
}
