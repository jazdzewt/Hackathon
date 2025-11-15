using Hackathon.Api.DTOs.Submissions;

namespace Hackathon.Api.Services;

public interface ISubmissionService
{
    Task<string> SubmitSolutionAsync(string challengeId, string userId, IFormFile file);
    Task<IEnumerable<SubmissionDto>> GetUserSubmissionsAsync(string userId);
    Task<IEnumerable<SubmissionDto>> GetChallengeSubmissionsAsync(string challengeId);
    Task EvaluateSubmissionAsync(string submissionId);
}
