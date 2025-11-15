namespace Hackathon.Api.Services;

public interface IScoringService
{
    /// <summary>
    /// Automatycznie ocenia submission porównując z ground-truth
    /// </summary>
    Task<decimal> EvaluateSubmissionAsync(string submissionId);

    /// <summary>
    /// Ręcznie ocenia submission (przez admina/sędziego)
    /// </summary>
    Task ManuallyScoreSubmissionAsync(string submissionId, decimal score, string? notes, string evaluatorId);

    /// <summary>
    /// Oblicza score na podstawie porównania dwóch plików
    /// </summary>
    Task<decimal> CalculateScoreAsync(byte[] submissionFile, byte[] groundTruthFile, string evaluationMetric, string fileExtension);
}
