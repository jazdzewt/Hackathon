using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.Services;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(ILeaderboardService leaderboardService, ILogger<LeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    /// <summary>
    /// Pobiera publiczną tablicę wyników dla wyzwania
    /// </summary>
    [HttpGet("{challengeId}")]
    public async Task<IActionResult> GetLeaderboard(string challengeId)
    {
        try
        {
            // Obsługuj zarówno int jak i UUID
            var leaderboard = await _leaderboardService.GetLeaderboardAsync(challengeId);
            return Ok(leaderboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching leaderboard for challenge {challengeId}");
            return StatusCode(500, new { error = "Error fetching leaderboard", details = ex.Message });
        }
    }
}
