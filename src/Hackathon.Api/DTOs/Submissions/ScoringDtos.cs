namespace Hackathon.Api.DTOs.Submissions;

public record SubmitChallengeDto(
    IFormFile File
);

public record EvaluationResultDto(
    string SubmissionId,
    decimal Score,
    string EvaluationStatus,
    string? ErrorMessage,
    DateTime EvaluatedAt
);

public record ManualScoreDto(
    decimal Score,
    string? Notes
);
