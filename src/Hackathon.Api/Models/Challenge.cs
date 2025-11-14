using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Hackathon.Api.Models;

[Table("challenges")]
public class Challenge : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("evaluation_metric")]
    public string EvaluationMetric { get; set; } = string.Empty;

    [Column("dataset_url")]
    public string? DatasetUrl { get; set; }

    [Column("submission_deadline")]
    public DateTime SubmissionDeadline { get; set; }

    [Column("max_file_size_mb")]
    public int MaxFileSizeMb { get; set; } = 100;

    [Column("allowed_file_types")]
    public string[]? AllowedFileTypes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
