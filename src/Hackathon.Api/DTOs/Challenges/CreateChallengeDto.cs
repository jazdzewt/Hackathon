namespace Hackathon.Api.DTOs.Challenges;

public record CreateChallengeDto(
    string Name,
    string ShortDescription,
    string FullDescription,
    string Rules,
    string EvaluationMetric,
    DateTime StartDate,
    DateTime? EndDate,
    int? MaxFileSizeMb,
    string[]? AllowedFileTypes
);
