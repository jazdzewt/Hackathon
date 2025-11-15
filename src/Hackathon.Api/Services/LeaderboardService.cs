using Hackathon.Api.DTOs.Leaderboard;
using Hackathon.Api.Models;
using Supabase;
using Supabase.Postgrest.Responses;
using Postgrest = Supabase.Postgrest;

namespace Hackathon.Api.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly Client _supabaseClient;

    public LeaderboardService(Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(int challengeId, int topN = 100)
    {
        return await GetLeaderboardAsync(challengeId.ToString(), topN);
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> GetLeaderboardAsync(string challengeId, int topN = 100)
    {
        // 1. Pobierz wszystkie submissions dla challenge
        var allSubmissions = await _supabaseClient
            .From<Submission>()
            .Where(s => s.ChallengeId == challengeId)
            .Get();

        // 2. Sortuj po wyniku (malejąco) i dacie (najnowsze najpierw)
        var sorted = allSubmissions.Models
            .OrderByDescending(s => s.Score.HasValue) // Najpierw z wynikami
            .ThenByDescending(s => s.Score ?? 0) // Potem po wyniku
            .ThenByDescending(s => s.SubmittedAt) // Na końcu po dacie
            .Take(topN)
            .ToList();

        // 3. Pobierz display names i utwórz wpisy leaderboard
        var leaderboard = new List<LeaderboardEntryDto>();
        int rank = 1;

        foreach (var submission in sorted)
        {
            // Pobierz display_name z Supabase Auth
            string displayName = submission.UserId; // fallback to userId
            try
            {
                var response = await _supabaseClient.Rpc("get_user_display_name", new Dictionary<string, object>
                {
                    { "user_uid", submission.UserId }
                });
                
                if (response != null)
                {
                    displayName = response.ToString() ?? submission.UserId;
                }
            }
            catch
            {
                displayName = submission.UserId;
            }

            leaderboard.Add(new LeaderboardEntryDto(
                Rank: rank++,
                Username: displayName,
                BestScore: submission.Score.HasValue ? (double)submission.Score.Value : null,
                TotalSubmissions: 1, // Każdy wpis to jedno submission
                LastSubmissionDate: submission.SubmittedAt,
                Status: submission.Status
            ));
        }

        return leaderboard;
    }

    public Task FreezeLeaderboardAsync(int challengeId)
    {
        throw new NotImplementedException();
    }
}
