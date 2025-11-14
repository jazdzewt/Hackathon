namespace Hackathon.Api.DTOs;

// Authentication
public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);

// Challenges
public record CreateChallengeRequest(
    string Title,
    string Description,
    string EvaluationMetric,
    DateTime SubmissionDeadline,
    string? DatasetUrl = null,
    int MaxFileSizeMb = 100,
    string[]? AllowedFileTypes = null
);

public record UpdateChallengeRequest(
    string? Title,
    string? Description,
    string? EvaluationMetric,
    DateTime? SubmissionDeadline,
    string? DatasetUrl,
    bool? IsActive
);

// Submissions
public record SubmitSolutionRequest(
    string ChallengeId,
    IFormFile File
);

// Responses
public record UserResponse(
    string Id,
    string Email,
    string Name,
    string Role,
    DateTime CreatedAt
);

public record ChallengeResponse(
    string Id,
    string Title,
    string Description,
    string EvaluationMetric,
    DateTime SubmissionDeadline,
    bool IsActive,
    string? DatasetUrl,
    int MaxFileSizeMb,
    string[]? AllowedFileTypes
);

public record SubmissionResponse(
    string Id,
    string UserId,
    string ChallengeId,
    string FileName,
    decimal? Score,
    string Status,
    DateTime SubmittedAt
);

public record LeaderboardEntry(
    string UserId,
    string UserName,
    string ChallengeId,
    decimal BestScore,
    DateTime LastUpdated
);
