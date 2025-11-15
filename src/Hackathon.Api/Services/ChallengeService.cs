using Hackathon.Api.DTOs.Challenges;
using Hackathon.Api.Models;
using Supabase;

namespace Hackathon.Api.Services;

public class ChallengeService : IChallengeService
{
    private readonly Client _supabaseClient;
    private const string DATASETS_BUCKET = "datasets";

    public ChallengeService(Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

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

    public async Task CreateChallengeAsync(Challenge challenge)
    {
        await _supabaseClient
            .From<Challenge>()
            .Insert(challenge);
    }

    public async Task<string> CreateChallengeWithDatasetAsync(Challenge challenge, byte[] datasetFile, string fileName)
    {
        // 1. Upload dataset do Supabase Storage
        var fileExtension = Path.GetExtension(fileName);
        var storedFileName = $"{challenge.Id}{fileExtension}";
        var filePath = $"challenges/{storedFileName}";

        await _supabaseClient.Storage
            .From(DATASETS_BUCKET)
            .Upload(datasetFile, filePath, new Supabase.Storage.FileOptions
            {
                ContentType = GetContentType(fileExtension),
                Upsert = false
            });

        // 2. Generuj publiczny URL
        var publicUrl = _supabaseClient.Storage
            .From(DATASETS_BUCKET)
            .GetPublicUrl(filePath);

        // 3. Zapisz URL w challenge
        challenge.DatasetUrl = publicUrl;

        // 4. Zapisz challenge do bazy
        await _supabaseClient
            .From<Challenge>()
            .Insert(challenge);

        return publicUrl;
    }

    public async Task UpdateChallengeAsync(string id, UpdateChallengeDto dto)
    {
        var existing = await _supabaseClient
            .From<Challenge>()
            .Where(c => c.Id == id)
            .Single();

        if (existing == null)
        {
            throw new KeyNotFoundException($"Challenge with id {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.Name)) existing.Title = dto.Name;
        if (!string.IsNullOrEmpty(dto.FullDescription)) existing.Description = dto.FullDescription;
        if (!string.IsNullOrEmpty(dto.EvaluationMetric)) existing.EvaluationMetric = dto.EvaluationMetric;
        if (dto.EndDate.HasValue) existing.SubmissionDeadline = dto.EndDate.Value;

        await _supabaseClient
            .From<Challenge>()
            .Update(existing);
    }

    public async Task DeleteChallengeAsync(string id)
    {
        await _supabaseClient
            .From<Challenge>()
            .Where(c => c.Id == id)
            .Delete();
    }

    public Task UploadGroundTruthAsync(int id, Stream fileStream, string fileName)
    {
        throw new NotImplementedException();
    }

    private static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            ".parquet" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }
}
