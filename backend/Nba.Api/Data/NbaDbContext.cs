using Microsoft.EntityFrameworkCore;
using Nba.Api.Models;

namespace Nba.Api.Data;

public class NbaDbContext(DbContextOptions<NbaDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<TeamPlayer> TeamPlayers => Set<TeamPlayer>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<ScoringEvent> ScoringEvents => Set<ScoringEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.Property(t => t.City).HasMaxLength(100).IsRequired();
            e.Property(t => t.Conference).HasMaxLength(10).IsRequired();
            e.Property(t => t.Division).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Player>(e =>
        {
            e.Property(p => p.FirstName).HasMaxLength(50).IsRequired();
            e.Property(p => p.LastName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<TeamPlayer>(e =>
        {
            e.HasOne(tp => tp.Team)
                .WithMany(t => t.TeamPlayers)
                .HasForeignKey(tp => tp.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(tp => tp.Player)
                .WithMany(p => p.TeamPlayers)
                .HasForeignKey(tp => tp.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(tp => new { tp.TeamId, tp.PlayerId, tp.StartDate }).IsUnique();
            // Speeds up the "active roster" lookup (EndDate IS NULL) per team.
            e.HasIndex(tp => new { tp.TeamId, tp.EndDate });
        });

        modelBuilder.Entity<Game>(e =>
        {
            e.Property(g => g.Season).HasMaxLength(7).IsRequired();
            e.Property(g => g.Arena).HasMaxLength(100);
            e.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(g => g.HomeTeam)
                .WithMany(t => t.HomeGames)
                .HasForeignKey(g => g.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(g => g.AwayTeam)
                .WithMany(t => t.AwayGames)
                .HasForeignKey(g => g.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(g => g.RowVersion).IsRowVersion();
            e.Property(g => g.Quarter).HasDefaultValue((byte)1);
            e.Property(g => g.ClockSecondsRemaining).HasDefaultValue(720);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Games_Quarter", "[Quarter] BETWEEN 1 AND 10");
                t.HasCheckConstraint("CK_Games_Clock", "[ClockSecondsRemaining] BETWEEN 0 AND 720");
            });
            e.HasIndex(g => g.GameDate);
            // A team's schedule is read by team, in date order.
            e.HasIndex(g => new { g.HomeTeamId, g.GameDate });
            e.HasIndex(g => new { g.AwayTeamId, g.GameDate });
        });

        modelBuilder.Entity<ScoringEvent>(e =>
        {
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ScoringEvents_Points", "[Points] IN (2, 3)");
                t.HasCheckConstraint("CK_ScoringEvents_Quarter", "[Quarter] BETWEEN 1 AND 10");
                t.HasCheckConstraint("CK_ScoringEvents_Clock", "[ClockSecondsRemaining] IS NULL OR [ClockSecondsRemaining] BETWEEN 0 AND 720");
            });
            e.Property(s => s.Quarter).HasDefaultValue((byte)1);
            e.Property(s => s.Points).HasColumnType("tinyint");
            e.Property(s => s.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(s => s.Game).WithMany().HasForeignKey(s => s.GameId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Team).WithMany().HasForeignKey(s => s.TeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Player).WithMany().HasForeignKey(s => s.PlayerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(s => new { s.GameId, s.CreatedAt });
        });
    }
}
