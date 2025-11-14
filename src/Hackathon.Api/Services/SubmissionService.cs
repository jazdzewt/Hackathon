using Hackathon.Api.DTOs.Submissions;

namespace Hackathon.Api.Services;

public class SubmissionService : ISubmissionService
{
    public Task<int> SubmitSolutionAsync(int challengeId, string userId, Stream fileStream, string fileName)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SubmissionDto>> GetUserSubmissionsAsync(string userId)
    {
        return Task.FromResult(Enumerable.Empty<SubmissionDto>());
    }

    public Task<IEnumerable<SubmissionDto>> GetChallengeSubmissionsAsync(int challengeId)
    {
        return Task.FromResult(Enumerable.Empty<SubmissionDto>());
    }

    public Task RejudgeSubmissionAsync(int submissionId)
    {
        throw new NotImplementedException();
    }
}
