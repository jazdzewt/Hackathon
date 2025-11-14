using Hackathon.Api.DTOs.Leaderboard;

namespace Hackathon.Api.Services;

public class LeaderboardService : ILeaderboardService
{
    public Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int challengeId, int topN = 100)
    {
        return Task.FromResult(Enumerable.Empty<LeaderboardEntryDto>());
    }

    public Task FreezeLeaderboardAsync(int challengeId)
    {
        throw new NotImplementedException();
    }
}
