using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Hackathon.Api.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("uid")]
    public string Uid { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = "user"; // 'user' lub 'admin'

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
