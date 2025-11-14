using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.DTOs.Leaderboard;
using Hackathon.Api.Services;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    /// <summary>
    /// Pobiera publiczną tablicę wyników dla wyzwania
    /// </summary>
    [HttpGet("{challengeId}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetLeaderboard(int challengeId)
    {
        // TODO: Implementacja pobierania tablicy wyników
        return Ok(new List<LeaderboardEntryDto>());
    }
}
