namespace Hackathon.Api.DTOs.Submissions;

public record SubmissionDto(
    int Id,
    int ChallengeId,
    string ChallengeName,
    DateTime SubmittedAt,
    string Status, // "Pending", "Processing", "Completed", "Failed"
    double? Score,
    string? ErrorMessage
);
