using Hackathon.Api.DTOs.Submissions;

namespace Hackathon.Api.Services;

public interface ISubmissionService
{
    Task<int> SubmitSolutionAsync(int challengeId, string userId, Stream fileStream, string fileName);
    Task<IEnumerable<SubmissionDto>> GetUserSubmissionsAsync(string userId);
    Task<IEnumerable<SubmissionDto>> GetChallengeSubmissionsAsync(int challengeId);
    Task RejudgeSubmissionAsync(int submissionId);
}
