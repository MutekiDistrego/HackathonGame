using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonGame.SessionService.Models;

[Table("team_members")]
public class TeamMember
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("team_id")]
    public long TeamId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("role")]
    public string? Role { get; set; }

    // Navigation
    [ForeignKey("TeamId")]
    public Team Team { get; set; } = null!;
}
