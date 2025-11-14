using Hackathon.Api.DTOs.Challenges;

namespace Hackathon.Api.Services;

public interface IChallengeService
{
    Task<IEnumerable<ChallengeListDto>> GetAllChallengesAsync();
    Task<ChallengeDetailDto?> GetChallengeByIdAsync(int id);
    Task<byte[]> GetChallengeDatasetAsync(int id);
    Task<int> CreateChallengeAsync(CreateChallengeDto dto);
    Task UpdateChallengeAsync(int id, UpdateChallengeDto dto);
    Task DeleteChallengeAsync(int id);
    Task UploadGroundTruthAsync(int id, Stream fileStream, string fileName);
}
