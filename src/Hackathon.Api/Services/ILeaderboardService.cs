using Hackathon.Api.DTOs.Leaderboard;

namespace Hackathon.Api.Services;

public interface ILeaderboardService
{
    Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int challengeId, int topN = 100);
    Task FreezeLeaderboardAsync(int challengeId);
}
