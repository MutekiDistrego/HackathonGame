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
    }
}
