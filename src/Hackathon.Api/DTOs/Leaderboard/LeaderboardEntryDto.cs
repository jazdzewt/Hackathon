namespace Hackathon.Api.DTOs.Leaderboard;

public record LeaderboardEntryDto(
    int Rank,
    string Username,
    double BestScore,
    int TotalSubmissions,
    DateTime LastSubmissionDate
);
