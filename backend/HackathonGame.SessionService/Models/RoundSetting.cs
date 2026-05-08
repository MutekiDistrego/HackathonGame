using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonGame.SessionService.Models;

[Table("round_settings")]
public class RoundSetting
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("round_number")]
    public int RoundNumber { get; set; }

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; } = 15;

    [MaxLength(100)]
    [Column("name")]
    public string? Name { get; set; }

    // Navigation
    [ForeignKey("SessionId")]
    public Session Session { get; set; } = null!;
}
