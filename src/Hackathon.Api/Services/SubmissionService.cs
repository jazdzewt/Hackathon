using System.Security.Cryptography;
using Hackathon.Api.DTOs.Submissions;
using Hackathon.Api.Models;
using Supabase;

namespace Hackathon.Api.Services;

public class SubmissionService : ISubmissionService
{
    private readonly Client _supabaseClient;
    private readonly IScoringService _scoringService;
    private readonly ILogger<SubmissionService> _logger;
    private const string SUBMISSIONS_BUCKET = "submissions";

    public SubmissionService(Client supabaseClient, IScoringService scoringService, ILogger<SubmissionService> logger)
    {
        _supabaseClient = supabaseClient;
        _scoringService = scoringService;
        _logger = logger;
    }

    public async Task<string> SubmitSolutionAsync(string challengeId, string userId, IFormFile file)
    {
        // 1. Walidacja challenge
        var challenge = await _supabaseClient
            .From<Challenge>()
            .Where(c => c.Id == challengeId)
            .Single();

        if (challenge == null)
        {
            throw new KeyNotFoundException($"Challenge {challengeId} not found");
        }

        if (!challenge.IsActive)
        {
            throw new InvalidOperationException("Challenge is not active");
        }

        if (DateTime.UtcNow > challenge.SubmissionDeadline)
        {
            throw new InvalidOperationException("Submission deadline has passed");
        }

        // 2. Walidacja pliku
        var fileExtension = Path.GetExtension(file.FileName);
        if (challenge.AllowedFileTypes != null && !challenge.AllowedFileTypes.Contains(fileExtension))
        {
            throw new ArgumentException($"File type {fileExtension} is not allowed for this challenge");
        }

        var fileSizeMb = file.Length / (1024.0m * 1024.0m);
        if (fileSizeMb > challenge.MaxFileSizeMb)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {challenge.MaxFileSizeMb}MB");
        }

        // 3. Oblicz hash pliku
        string fileHash;
        using (var stream = file.OpenReadStream())
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            fileHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // 4. Sprawdź czy submission z takim hashem już istnieje dla tego użytkownika
        var existingSubmission = await _supabaseClient
            .From<Submission>()
            .Where(s => s.UserId == userId)
            .Where(s => s.ChallengeId == challengeId)
            .Where(s => s.FileHash == fileHash)
            .Get();

        if (existingSubmission.Models.Any())
        {
            throw new InvalidOperationException("This file has already been submitted");
        }

        // 5. Upload pliku do Storage
        var submissionId = Guid.NewGuid().ToString();
        var storedFileName = $"{userId}/{challengeId}/{submissionId}{fileExtension}";

        byte[] fileBytes;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
        }

        await _supabaseClient.Storage
            .From(SUBMISSIONS_BUCKET)
            .Upload(fileBytes, storedFileName, new Supabase.Storage.FileOptions
            {
                ContentType = GetContentType(fileExtension),
                Upsert = false
            });

        var fileUrl = _supabaseClient.Storage
            .From(SUBMISSIONS_BUCKET)
            .GetPublicUrl(storedFileName);

        // 6. Stwórz submission w bazie
        var submission = new Submission
        {
            Id = submissionId,
            UserId = userId,
            ChallengeId = challengeId,
            FileName = file.FileName,
            FileUrl = fileUrl,
            FileSizeMb = fileSizeMb,
            FileHash = fileHash,
            Status = "pending",
            SubmittedAt = DateTime.UtcNow
        };

        await _supabaseClient
            .From<Submission>()
            .Insert(submission);

        _logger.LogInformation($"Submission {submissionId} created for user {userId} on challenge {challengeId}");

        // 7. Asynchroniczne uruchomienie oceniania w tle
        _ = Task.Run(async () =>
        {
            try
            {
                await EvaluateSubmissionAsync(submissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Background evaluation failed for submission {submissionId}");
            }
        });

        return submissionId;
    }

    public async Task<IEnumerable<SubmissionDto>> GetUserSubmissionsAsync(string userId)
    {
        var response = await _supabaseClient
            .From<Submission>()
            .Where(s => s.UserId == userId)
            .Get();

        return response.Models
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionDto(
                s.Id,
                s.ChallengeId,
                s.FileName,
                s.Score,
                s.Status,
                s.SubmittedAt
            ));
    }

    public async Task<IEnumerable<SubmissionDto>> GetChallengeSubmissionsAsync(string challengeId)
    {
        var response = await _supabaseClient
            .From<Submission>()
            .Where(s => s.ChallengeId == challengeId)
            .Get();

        return response.Models
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmissionDto(
                s.Id,
                s.ChallengeId,
                s.FileName,
                s.Score,
                s.Status,
                s.SubmittedAt
            ));
    }

    public async Task EvaluateSubmissionAsync(string submissionId)
    {
        await _scoringService.EvaluateSubmissionAsync(submissionId);
    }

    private static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
