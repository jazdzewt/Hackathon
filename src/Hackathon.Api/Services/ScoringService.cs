using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hackathon.Api.Models;
using Supabase;

namespace Hackathon.Api.Services;

public class ScoringService : IScoringService
{
    private readonly Client _supabaseClient;
    private readonly ILogger<ScoringService> _logger;

    public ScoringService(Client supabaseClient, ILogger<ScoringService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<decimal> EvaluateSubmissionAsync(string submissionId)
    {
        // Pobierz submission
        var response = await _supabaseClient
            .From<Submission>()
            .Where(s => s.Id == submissionId)
            .Get();

        var submission = response.Models.FirstOrDefault();

        if (submission == null)
        {
            _logger.LogError($"Submission {submissionId} not found in database");
            throw new KeyNotFoundException($"Submission {submissionId} not found");
        }

        return await EvaluateSubmissionAsync(submission);
    }

    public async Task<decimal> EvaluateSubmissionAsync(Submission submission)
    {
        try
        {
            _logger.LogInformation($"Starting evaluation for submission {submission.Id}");

            // 2. Zaktualizuj status na "processing"
            submission.Status = "processing";
            await _supabaseClient.From<Submission>().Update(submission);

            // 3. Pobierz challenge
            var challenge = await _supabaseClient
                .From<Challenge>()
                .Where(c => c.Id == submission.ChallengeId)
                .Single();

            if (challenge == null)
            {
                throw new KeyNotFoundException($"Challenge {submission.ChallengeId} not found");
            }

            // 4. Sprawdź czy challenge ma ground truth
            if (string.IsNullOrEmpty(challenge.GroundTruthUrl))
            {
                throw new InvalidOperationException($"Ground truth not found for challenge {challenge.Id}");
            }

            // 5. Pobierz pliki
            var submissionFile = await DownloadFileAsync(submission.FileUrl);
            var groundTruthFile = await DownloadFileAsync(challenge.GroundTruthUrl);

            // 6. Oblicz hash submission (dla deterministyczności)
            submission.FileHash = CalculateFileHash(submissionFile);

            // 6b. Anti-cheat: Validate row count
            var fileExtension = Path.GetExtension(submission.FileName);
            var submissionRowCount = CountRows(submissionFile, fileExtension);
            var groundTruthRowCount = CountRows(groundTruthFile, fileExtension);
            
            submission.RowCount = submissionRowCount;
            
            if (submissionRowCount != groundTruthRowCount)
            {
                throw new ArgumentException($"Row count mismatch: submission has {submissionRowCount} rows, expected {groundTruthRowCount}");
            }

            // 7. Oblicz score
            var score = await CalculateScoreAsync(submissionFile, groundTruthFile, challenge.EvaluationMetric, fileExtension);

            // 8. Anti-cheat: Flag suspicious perfect or near-perfect scores
            bool isSuspicious = score >= 99.5m;
            submission.IsSuspicious = isSuspicious;
            
            if (isSuspicious)
            {
                _logger.LogWarning($"Suspicious score detected for submission {submissionId}: {score}% (flagged for manual review)");
            }

            // 9. Zapisz wynik
            submission.Score = score;
            submission.Status = "completed";

            await _supabaseClient.From<Submission>().Update(submission);

            // 9. Aktualizuj leaderboard
            await UpdateLeaderboardAsync(submission.UserId, submission.ChallengeId, score, submission.Id);

            _logger.LogInformation($"Submission {submission.Id} evaluated with score: {score}");

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error evaluating submission {submission.Id}");

            // Zaktualizuj status na "failed"
            var submissionToUpdate = await _supabaseClient
                .From<Submission>()
                .Where(s => s.Id == submission.Id)
                .Get();

            var sub = submissionToUpdate.Models.FirstOrDefault();
            if (sub != null)
            {
                sub.Status = "failed";
                sub.ErrorMessage = ex.Message;
                await _supabaseClient.From<Submission>().Update(sub);
            }

            throw;
        }
    }

    public async Task ManuallyScoreSubmissionAsync(string submissionId, decimal score, string? notes, string evaluatorId)
    {
        var submission = await _supabaseClient
            .From<Submission>()
            .Where(s => s.Id == submissionId)
            .Single();

        if (submission == null)
        {
            throw new KeyNotFoundException($"Submission {submissionId} not found");
        }

        submission.Score = score;
        submission.Status = "completed";

        await _supabaseClient.From<Submission>().Update(submission);

        _logger.LogInformation($"Submission {submissionId} manually scored by {evaluatorId} with score: {score}");
    }

    public async Task<decimal> CalculateScoreAsync(byte[] submissionFile, byte[] groundTruthFile, string evaluationMetric, string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".csv" => await CalculateScoreFromCsvAsync(submissionFile, groundTruthFile, evaluationMetric),
            ".json" => await CalculateScoreFromJsonAsync(submissionFile, groundTruthFile, evaluationMetric),
            ".txt" => await CalculateScoreFromTextAsync(submissionFile, groundTruthFile, evaluationMetric),
            _ => throw new NotSupportedException($"File type {fileExtension} is not supported for evaluation")
        };
    }

    private async Task<decimal> CalculateScoreFromCsvAsync(byte[] submissionFile, byte[] groundTruthFile, string evaluationMetric)
    {
        var submissionLines = Encoding.UTF8.GetString(submissionFile).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var groundTruthLines = Encoding.UTF8.GetString(groundTruthFile).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Pomijamy nagłówek jeśli istnieje
        var submissionData = submissionLines.Skip(1).Select(line => line.Split(',').Last().Trim()).ToList();
        var groundTruthData = groundTruthLines.Skip(1).Select(line => line.Split(',').Last().Trim()).ToList();

        if (submissionData.Count != groundTruthData.Count)
        {
            throw new ArgumentException($"Data length mismatch: submission={submissionData.Count}, ground_truth={groundTruthData.Count}");
        }

        return evaluationMetric.ToLowerInvariant() switch
        {
            "accuracy" => await Task.FromResult(CalculateAccuracy(submissionData, groundTruthData)),
            "f1" or "f1-score" => await Task.FromResult(CalculateF1Score(submissionData, groundTruthData)),
            "mse" or "mean-squared-error" => await Task.FromResult(CalculateMse(submissionData, groundTruthData)),
            "mae" or "mean-absolute-error" => await Task.FromResult(CalculateMae(submissionData, groundTruthData)),
            "rmse" or "root-mean-squared-error" => await Task.FromResult(CalculateRmse(submissionData, groundTruthData)),
            _ => await Task.FromResult(CalculateAccuracy(submissionData, groundTruthData)) // default
        };
    }

    private async Task<decimal> CalculateScoreFromJsonAsync(byte[] submissionFile, byte[] groundTruthFile, string evaluationMetric)
    {
        var submissionJson = JsonDocument.Parse(submissionFile).RootElement;
        var groundTruthJson = JsonDocument.Parse(groundTruthFile).RootElement;

        if (!submissionJson.TryGetProperty("predictions", out var subPredictions) ||
            !groundTruthJson.TryGetProperty("predictions", out var refPredictions))
        {
            throw new ArgumentException("Missing 'predictions' array in JSON");
        }

        var submissionData = subPredictions.EnumerateArray().Select(e => e.ToString()).ToList();
        var groundTruthData = refPredictions.EnumerateArray().Select(e => e.ToString()).ToList();

        if (submissionData.Count != groundTruthData.Count)
        {
            throw new ArgumentException($"Data length mismatch: submission={submissionData.Count}, ground_truth={groundTruthData.Count}");
        }

        return evaluationMetric.ToLowerInvariant() switch
        {
            "accuracy" => await Task.FromResult(CalculateAccuracy(submissionData, groundTruthData)),
            "f1" or "f1-score" => await Task.FromResult(CalculateF1Score(submissionData, groundTruthData)),
            "mse" or "mean-squared-error" => await Task.FromResult(CalculateMse(submissionData, groundTruthData)),
            "mae" or "mean-absolute-error" => await Task.FromResult(CalculateMae(submissionData, groundTruthData)),
            _ => await Task.FromResult(CalculateAccuracy(submissionData, groundTruthData))
        };
    }

    private async Task<decimal> CalculateScoreFromTextAsync(byte[] submissionFile, byte[] groundTruthFile, string evaluationMetric)
    {
        var submissionLines = Encoding.UTF8.GetString(submissionFile).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var groundTruthLines = Encoding.UTF8.GetString(groundTruthFile).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (submissionLines.Length != groundTruthLines.Length)
        {
            throw new ArgumentException($"Data length mismatch: submission={submissionLines.Length}, ground_truth={groundTruthLines.Length}");
        }

        var submissionData = submissionLines.Select(l => l.Trim()).ToList();
        var groundTruthData = groundTruthLines.Select(l => l.Trim()).ToList();

        return await Task.FromResult(CalculateAccuracy(submissionData, groundTruthData));
    }

    // Metryki ewaluacji

    private decimal CalculateAccuracy(List<string> submission, List<string> groundTruth)
    {
        int correct = 0;
        for (int i = 0; i < submission.Count; i++)
        {
            if (submission[i].Equals(groundTruth[i], StringComparison.OrdinalIgnoreCase))
            {
                correct++;
            }
        }

        return submission.Count > 0 ? Math.Round((decimal)correct / submission.Count * 100, 2) : 0;
    }

    private decimal CalculateF1Score(List<string> submission, List<string> groundTruth)
    {
        int truePositives = 0;
        int falsePositives = 0;
        int falseNegatives = 0;

        for (int i = 0; i < submission.Count; i++)
        {
            var predicted = submission[i].Trim();
            var actual = groundTruth[i].Trim();

            if (predicted == "1" && actual == "1") truePositives++;
            else if (predicted == "1" && actual == "0") falsePositives++;
            else if (predicted == "0" && actual == "1") falseNegatives++;
        }

        if (truePositives == 0)
        {
            return 0m;
        }

        decimal precision = (decimal)truePositives / (truePositives + falsePositives);
        decimal recall = (decimal)truePositives / (truePositives + falseNegatives);
        decimal f1 = 2 * (precision * recall) / (precision + recall) * 100;

        return Math.Round(f1, 2);
    }

    private decimal CalculateMse(List<string> submission, List<string> groundTruth)
    {
        double sumSquaredErrors = 0;

        for (int i = 0; i < submission.Count; i++)
        {
            if (!double.TryParse(submission[i], out var predicted) || !double.TryParse(groundTruth[i], out var actual))
            {
                throw new ArgumentException("Values must be numeric for MSE calculation");
            }

            double error = predicted - actual;
            sumSquaredErrors += error * error;
        }

        double mse = sumSquaredErrors / submission.Count;

        // Konwersja MSE na score (0-100): im mniejszy MSE tym lepiej
        // Score = 100 * exp(-MSE)
        decimal score = (decimal)(100 * Math.Exp(-mse));
        return Math.Round(score, 2);
    }

    private decimal CalculateMae(List<string> submission, List<string> groundTruth)
    {
        double sumAbsoluteErrors = 0;

        for (int i = 0; i < submission.Count; i++)
        {
            if (!double.TryParse(submission[i], out var predicted) || !double.TryParse(groundTruth[i], out var actual))
            {
                throw new ArgumentException("Values must be numeric for MAE calculation");
            }

            sumAbsoluteErrors += Math.Abs(predicted - actual);
        }

        double mae = sumAbsoluteErrors / submission.Count;

        // Konwersja MAE na score
        decimal score = (decimal)(100 * Math.Exp(-mae));
        return Math.Round(score, 2);
    }

    private decimal CalculateRmse(List<string> submission, List<string> groundTruth)
    {
        double sumSquaredErrors = 0;

        for (int i = 0; i < submission.Count; i++)
        {
            if (!double.TryParse(submission[i], out var predicted) || !double.TryParse(groundTruth[i], out var actual))
            {
                throw new ArgumentException("Values must be numeric for RMSE calculation");
            }

            double error = predicted - actual;
            sumSquaredErrors += error * error;
        }

        double rmse = Math.Sqrt(sumSquaredErrors / submission.Count);

        // Konwersja RMSE na score
        decimal score = (decimal)(100 * Math.Exp(-rmse));
        return Math.Round(score, 2);
    }

    // Pomocnicze metody

    private async Task<byte[]> DownloadFileAsync(string fileUrl)
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetByteArrayAsync(fileUrl);
    }

    private string CalculateFileHash(byte[] fileData)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileData);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private int CountRows(byte[] fileData, string fileExtension)
    {
        var content = Encoding.UTF8.GetString(fileData);
        
        return fileExtension.ToLowerInvariant() switch
        {
            ".csv" or ".txt" => content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1, // -1 for header
            ".json" => JsonDocument.Parse(fileData).RootElement.GetProperty("predictions").GetArrayLength(),
            _ => 0
        };
    }

    private async Task UpdateLeaderboardAsync(string userId, string challengeId, decimal score, string submissionId)
    {
        // Sprawdź czy użytkownik już ma wpis w leaderboard dla tego challenge
        var existingEntry = await _supabaseClient
            .From<Leaderboard>()
            .Where(l => l.UserId == userId)
            .Where(l => l.ChallengeId == challengeId)
            .Single();

        if (existingEntry == null)
        {
            // Nowy wpis
            var newEntry = new Leaderboard
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                ChallengeId = challengeId,
                BestScore = score,
                SubmissionId = submissionId,
                LastUpdated = DateTime.UtcNow
            };

            await _supabaseClient.From<Leaderboard>().Insert(newEntry);
            _logger.LogInformation($"New leaderboard entry created for user {userId} in challenge {challengeId} with score {score}");
        }
        else if (score > existingEntry.BestScore)
        {
            // Aktualizuj jeśli nowy wynik jest lepszy
            existingEntry.BestScore = score;
            existingEntry.SubmissionId = submissionId;
            existingEntry.LastUpdated = DateTime.UtcNow;

            await _supabaseClient.From<Leaderboard>().Update(existingEntry);
            _logger.LogInformation($"Leaderboard updated for user {userId} in challenge {challengeId}: {existingEntry.BestScore} -> {score}");
        }
    }
}
