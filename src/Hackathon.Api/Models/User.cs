// Models/User.cs
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Hackathon.Api.Models;

[Table("users")]
public class User : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("name")]
    public string? Name { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}