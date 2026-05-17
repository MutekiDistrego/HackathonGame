using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonGame.SessionService.Models;

[Table("round_history")]
public class RoundHistory
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("session_id")]
    public long SessionId { get; set; }

    [Column("round_number")]
    public int RoundNumber { get; set; }

    [Column("team_count")]
    public int TeamCount { get; set; }

    [MaxLength(1)]
    [Column("track")]
    public string? Track { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [Column("actual_duration_minutes")]
    public decimal? ActualDurationMinutes { get; set; }

    [Column("planned_duration_minutes")]
    public int? PlannedDurationMinutes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("SessionId")]
    public Session Session { get; set; } = null!;
}