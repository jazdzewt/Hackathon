using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Hackathon.Api.Models;

[Table("leaderboard")]
public class Leaderboard : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("challenge_id")]
    public string ChallengeId { get; set; } = string.Empty;

    [Column("best_score")]
    public decimal BestScore { get; set; }

    [Column("submission_id")]
    public string SubmissionId { get; set; } = string.Empty;

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
