namespace Hackathon.Api.DTOs.Leaderboard;

public record LeaderboardEntryDto(
    int Rank,
    string Username,
    double? BestScore, // nullable - może być null jeśli użytkownik ma tylko pending submissions
    int TotalSubmissions,
    DateTime LastSubmissionDate,
    string? Status = null // "completed", "pending", "processing", "failed"
);
