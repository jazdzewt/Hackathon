using Hackathon.Api.DTOs.Challenges;
using Hackathon.Api.Models;

namespace Hackathon.Api.Services;

public interface IChallengeService
{
    Task<IEnumerable<ChallengeListDto>> GetAllChallengesAsync();
    Task<ChallengeDetailDto?> GetChallengeByIdAsync(int id);
    Task<byte[]> GetChallengeDatasetAsync(int id);
    Task CreateChallengeAsync(Challenge challenge);
    Task<string> CreateChallengeWithDatasetAsync(Challenge challenge, byte[] datasetFile, string fileName);
    Task UpdateChallengeAsync(string id, UpdateChallengeDto dto);
    Task DeleteChallengeAsync(string id);
    Task<string> UploadGroundTruthAsync(string challengeId, byte[] fileBytes, string fileName);
    Task<string> UploadDatasetAsync(string challengeId, byte[] fileBytes, string fileName);
}
