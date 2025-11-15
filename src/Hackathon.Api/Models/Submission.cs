using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Hackathon.Api.Models;

[Table("submissions")]
public class Submission : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("challenge_id")]
    public string ChallengeId { get; set; } = string.Empty;

    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Column("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [Column("file_size_mb")]
    public decimal? FileSizeMb { get; set; }

    [Column("file_hash")]
    public string? FileHash { get; set; }

    [Column("score")]
    public decimal? Score { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [Column("is_suspicious")]
    public bool? IsSuspicious { get; set; }

    [Column("row_count")]
    public int? RowCount { get; set; }
}
