namespace Hackathon.Api.DTOs.Submissions;

public record SubmissionDto(
    string Id,
    string ChallengeId,
    string FileName,
    decimal? Score,
    string Status,
    DateTime SubmittedAt
);
