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
        var response = await _supabaseClient
            .From<Challenge>()
            .Insert(challenge);
        
        // Supabase zwraca wstawiony obiekt z faktycznym ID
        var insertedChallenge = response.Models.FirstOrDefault();
        if (insertedChallenge != null)
        {
            challenge.Id = insertedChallenge.Id;
        }
    }

    public async Task<string> CreateChallengeWithDatasetAsync(Challenge challenge, byte[] datasetFile, string fileName)
    {
        // 1. Zapisz challenge do bazy NAJPIERW, aby uzyskać wygenerowane ID
        var response = await _supabaseClient
            .From<Challenge>()
            .Insert(challenge);
        
        var insertedChallenge = response.Models.FirstOrDefault();
        if (insertedChallenge == null)
        {
            throw new Exception("Failed to create challenge in database");
        }
        
        challenge.Id = insertedChallenge.Id;

        // 2. Upload dataset do Supabase Storage używając faktycznego ID
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

        // 3. Generuj publiczny URL
        var publicUrl = _supabaseClient.Storage
            .From(DATASETS_BUCKET)
            .GetPublicUrl(filePath);

        // 4. Aktualizuj challenge z DatasetUrl
        challenge.DatasetUrl = publicUrl;
        await _supabaseClient
            .From<Challenge>()
            .Where(c => c.Id == challenge.Id)
            .Update(challenge);

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
        existing.IsActive = dto.IsActive;

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

    public async Task<string> UploadGroundTruthAsync(string challengeId, byte[] fileBytes, string fileName)
    {
        var fileExtension = Path.GetExtension(fileName);
        var storedFileName = $"{challengeId}{fileExtension}";
        var filePath = $"ground-truth/{storedFileName}";

        await _supabaseClient.Storage
            .From(DATASETS_BUCKET)
            .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions
            {
                ContentType = GetContentType(fileExtension),
                Upsert = true
            });

        var publicUrl = _supabaseClient.Storage
            .From(DATASETS_BUCKET)
            .GetPublicUrl(filePath);

        Console.WriteLine($"[GROUND TRUTH] Public URL: {publicUrl}");
        Console.WriteLine($"[GROUND TRUTH] Updating challenge {challengeId}");

        // Pobierz challenge z bazy
        var challenge = await _supabaseClient
            .From<Challenge>()
            .Where(c => c.Id == challengeId)
            .Single();

        if (challenge == null)
        {
            throw new KeyNotFoundException($"Challenge with id {challengeId} not found");
        }

        // Aktualizuj pole GroundTruthUrl
        challenge.GroundTruthUrl = publicUrl;
        
        Console.WriteLine($"[GROUND TRUTH] Before update - GroundTruthUrl: '{challenge.GroundTruthUrl}'");

        // Zapisz zmiany
        await _supabaseClient
            .From<Challenge>()
            .Update(challenge);

        Console.WriteLine($"[GROUND TRUTH] Challenge {challengeId} updated successfully");

        // Zweryfikuj czy zapisało się poprawnie
        var updatedChallenge = await _supabaseClient
            .From<Challenge>()
            .Where(c => c.Id == challengeId)
            .Single();
        
        Console.WriteLine($"[GROUND TRUTH] Verification - GroundTruthUrl in DB: '{updatedChallenge?.GroundTruthUrl}'");

        return publicUrl;
    }

    public async Task<string> UploadDatasetAsync(string challengeId, byte[] fileBytes, string fileName)
    {
        var fileExtension = Path.GetExtension(fileName);
        var storedFileName = $"{challengeId}{fileExtension}";
        var filePath = $"challenges/{storedFileName}";

        Console.WriteLine($"[DATASET] Uploading to path: {filePath}");

        await _supabaseClient.Storage
            .From(DATASETS_BUCKET)
            .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions
            {
                ContentType = GetContentType(fileExtension),
                Upsert = true
            });

        var publicUrl = _supabaseClient.Storage
            .From(DATASETS_BUCKET)
            .GetPublicUrl(filePath);

        Console.WriteLine($"[DATASET] Public URL: {publicUrl}");
        Console.WriteLine($"[DATASET] Updating challenge {challengeId}");

        // Pobierz challenge z bazy
        var challenge = await _supabaseClient
            .From<Challenge>()
            .Where(c => c.Id == challengeId)
            .Single();

        if (challenge == null)
        {
            throw new KeyNotFoundException($"Challenge with id {challengeId} not found");
        }

        // Aktualizuj pole DatasetUrl
        challenge.DatasetUrl = publicUrl;
        
        Console.WriteLine($"[DATASET] Before update - DatasetUrl: '{challenge.DatasetUrl}'");

        // Zapisz zmiany
        await _supabaseClient
            .From<Challenge>()
            .Update(challenge);

        Console.WriteLine($"[DATASET] Challenge {challengeId} updated successfully");

        // Zweryfikuj czy zapisało się poprawnie
        var updatedChallenge = await _supabaseClient
            .From<Challenge>()
            .Where(c => c.Id == challengeId)
            .Single();
        
        Console.WriteLine($"[DATASET] Verification - DatasetUrl in DB: '{updatedChallenge?.DatasetUrl}'");

        return publicUrl;
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
