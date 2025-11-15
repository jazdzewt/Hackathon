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

        // 3. Pobierz unique user IDs
        var uniqueUserIds = sorted.Select(s => s.UserId).Distinct().ToList();
        
        // 4. Pobierz display names dla wszystkich użytkowników
        var userDisplayNames = new Dictionary<string, string>();
        foreach (var userId in uniqueUserIds)
        {
            string displayName = await GetUserDisplayNameAsync(userId);
            userDisplayNames[userId] = displayName;
        }

        // 5. Utwórz wpisy leaderboard
        var leaderboard = new List<LeaderboardEntryDto>();
        int rank = 1;

        foreach (var submission in sorted)
        {
            var displayName = userDisplayNames.GetValueOrDefault(submission.UserId, submission.UserId);

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

    private async Task<string> GetUserDisplayNameAsync(string userId)
    {
        try
        {
            Console.WriteLine($"[LEADERBOARD] Fetching display name for user: {userId}");
            
            var rpcParams = new Dictionary<string, object>
            {
                { "user_uid", userId }
            };
            
            var result = await _supabaseClient.Rpc("get_user_email", rpcParams);
            
            Console.WriteLine($"[LEADERBOARD] RPC result type: {result?.GetType().Name}");
            Console.WriteLine($"[LEADERBOARD] RPC result content: {result?.Content}");
            
            if (result != null && result.Content != null)
            {
                // Usuń cudzysłowy z JSON stringa jeśli są
                var displayName = result.Content.Trim('"');
                Console.WriteLine($"[LEADERBOARD] Display name for {userId}: {displayName}");
                
                if (!string.IsNullOrEmpty(displayName) && displayName != "null")
                {
                    return displayName;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LEADERBOARD] Error fetching user display name for {userId}: {ex.Message}");
            Console.WriteLine($"[LEADERBOARD] Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine($"[LEADERBOARD] Falling back to userId: {userId}");
        return userId;
    }

    public Task FreezeLeaderboardAsync(int challengeId)
    {
        throw new NotImplementedException();
    }
}
