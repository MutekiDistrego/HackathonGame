using Microsoft.EntityFrameworkCore;
using HackathonGame.SessionService.Models;

namespace HackathonGame.SessionService.Data;

public class SessionDbContext : DbContext
{
    public SessionDbContext(DbContextOptions<SessionDbContext> options) : base(options) { }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<RoundSetting> RoundSettings => Set<RoundSetting>();

    // ── НОВЕ: таблиця для ML-сервісу ────────────────────────
    public DbSet<RoundHistory> RoundHistory => Set<RoundHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Status).HasDefaultValue("WAITING");
            entity.Property(e => e.CurrentRound).HasDefaultValue(1);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasOne(t => t.Session)
                  .WithMany(s => s.Teams)
                  .HasForeignKey(t => t.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.LifeTokens).HasDefaultValue(3);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasOne(m => m.Team)
                  .WithMany(t => t.Members)
                  .HasForeignKey(m => m.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoundSetting>(entity =>
        {
            entity.HasOne(r => r.Session)
                  .WithMany(s => s.RoundSettings)
                  .HasForeignKey(r => r.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.DurationMinutes).HasDefaultValue(15);
        });

        // ── НОВЕ: конфігурація RoundHistory ─────────────────
        modelBuilder.Entity<RoundHistory>(entity =>
        {
            entity.HasOne(r => r.Session)
                  .WithMany()
                  .HasForeignKey(r => r.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            // Індекс для ML-запитів (round_number, team_count, track)
            entity.HasIndex(e => new { e.RoundNumber, e.TeamCount, e.Track })
                  .HasFilter("actual_duration_minutes IS NOT NULL")
                  .HasDatabaseName("idx_round_history_ml");

            entity.HasIndex(e => e.SessionId)
                  .HasDatabaseName("idx_round_history_session");
        });
    }
}