using Microsoft.AspNetCore.Mvc;
using Hackathon.Api.Models;
using Hackathon.Api.DTOs;
using Supabase;

namespace Hackathon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly Client _supabase;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(Client supabase, ILogger<LeaderboardController> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    /// <summary>
    /// Pobiera publiczną tablicę wyników dla wyzwania
    /// </summary>
    [HttpGet("{challengeId}")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntry>>> GetLeaderboard(string challengeId)
    {
        try
        {
            _logger.LogInformation($"Pobieranie leaderboard dla wyzwania: {challengeId}");

            // Pobierz wyniki posortowane od najlepszego
            var leaderboardResponse = await _supabase
                .From<Leaderboard>()
                .Where(l => l.ChallengeId == challengeId)
                .Get();

            var results = new List<LeaderboardEntry>();

            // Dla każdego wyniku pobierz informacje o użytkowniku
            foreach (var entry in leaderboardResponse.Models)
            {
                var userResponse = await _supabase
                    .From<User>()
                    .Where(u => u.Id == entry.UserId)
                    .Single();

                if (userResponse != null)
                {
                    results.Add(new LeaderboardEntry(
                        entry.UserId,
                        userResponse.Name ?? "Nieznany",
                        entry.ChallengeId,
                        entry.BestScore,
                        entry.LastUpdated
                    ));
                }
            }

            // Sortuj wyniki (od najwyższego do najniższego)
            // TODO: Uwzględnij typ metryki (np. RMSE -> niższy lepszy, Accuracy -> wyższy lepszy)
            var sorted = results.OrderByDescending(r => r.BestScore).ToList();

            return Ok(sorted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd pobierania leaderboard");
            return BadRequest(new { error = "Błąd pobierania leaderboard", details = ex.Message });
        }
    }
}
