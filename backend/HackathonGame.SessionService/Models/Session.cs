using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonGame.SessionService.Models;

[Table("sessions")]
public class Session
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(6)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "WAITING";

    [Column("current_round")]
    public int CurrentRound { get; set; } = 1;

    [Column("round_end_time")]
    public DateTime? RoundEndTime { get; set; }

    [Column("game_end_time")]
    public DateTime? GameEndTime { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("admin_password")]
    public string AdminPassword { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<RoundSetting> RoundSettings { get; set; } = new List<RoundSetting>();
}
