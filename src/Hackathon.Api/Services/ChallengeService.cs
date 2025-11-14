using Hackathon.Api.DTOs.Challenges;

namespace Hackathon.Api.Services;

public class ChallengeService : IChallengeService
{
    public Task<IEnumerable<ChallengeListDto>> GetAllChallengesAsync()
    {
        return Task.FromResult(Enumerable.Empty<ChallengeListDto>());
    }

    public Task<ChallengeDetailDto?> GetChallengeByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<byte[]> GetChallengeDatasetAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<int> CreateChallengeAsync(CreateChallengeDto dto)
    {
        throw new NotImplementedException();
    }

    public Task UpdateChallengeAsync(int id, UpdateChallengeDto dto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteChallengeAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task UploadGroundTruthAsync(int id, Stream fileStream, string fileName)
    {
        throw new NotImplementedException();
    }
}
