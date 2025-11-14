namespace Hackathon.Api.DTOs.Challenges;

public record ChallengeDetailDto(
    int Id,
    string Name,
    string FullDescription,
    string Rules,
    string EvaluationMetric,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    int TotalSubmissions,
    int TotalParticipants
);
