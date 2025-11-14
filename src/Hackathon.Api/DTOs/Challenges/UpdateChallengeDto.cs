namespace Hackathon.Api.DTOs.Challenges;

public record UpdateChallengeDto(
    string Name,
    string ShortDescription,
    string FullDescription,
    string Rules,
    string EvaluationMetric,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive
);
