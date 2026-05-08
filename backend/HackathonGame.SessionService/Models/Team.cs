using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonGame.SessionService.Models;

[Table("teams")]
public class Team
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("life_tokens")]
    public int LifeTokens { get; set; } = 3;

    [MaxLength(1)]
    [Column("selected_track")]
    public string? SelectedTrack { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SessionId")]
    public Session Session { get; set; } = null!;

    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}
