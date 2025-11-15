using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Hackathon.Api.Models;

[Table("challenge_assets")]
public class ChallengeAsset : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("challenge_id")]
    public string ChallengeId { get; set; } = string.Empty;

    [Column("asset_type")]
    public string AssetType { get; set; } = string.Empty; // 'ground_truth', 'dataset', 'sample_submission'

    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Column("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [Column("file_size_mb")]
    public decimal? FileSizeMb { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; } = false; // ground_truth zawsze false!

    [Column("uploaded_by")]
    public string? UploadedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
