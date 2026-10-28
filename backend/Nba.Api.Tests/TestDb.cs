using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nba.Api.Data;
using Nba.Api.Models;

namespace Nba.Api.Tests;

// In-memory SQLite database with the real model, seeded with two teams (1 = Lakers, 2 = Celtics) and a third (3 = Bulls).
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    public NbaDbContext Db { get; }

    public ThrowOnceInterceptor Conflict { get; } = new();

    public TestDb()
    {
        _connection.Open();
        Db = new NbaDbContext(new DbContextOptionsBuilder<NbaDbContext>()
            .UseSqlite(_connection).AddInterceptors(Conflict).Options);
        Db.Database.EnsureCreated();

        Db.Teams.AddRange(
            new Team { Id = 1, City = "Los Angeles", Name = "Lakers", Conference = "", Division = "" },
            new Team { Id = 2, City = "Boston", Name = "Celtics", Conference = "", Division = "" },
            new Team { Id = 3, City = "Chicago", Name = "Bulls", Conference = "", Division = "" });
        Db.SaveChanges();
    }

    public Player AddPlayer(int id, string first, string last, int? teamId, bool active = true)
    {
        var p = new Player { Id = id, FirstName = first, LastName = last };
        Db.Players.Add(p);
        if (teamId is not null)
            Db.TeamPlayers.Add(new TeamPlayer
            {
                PlayerId = id, TeamId = teamId.Value,
                StartDate = new DateOnly(2026, 1, 1), EndDate = active ? null : new DateOnly(2026, 3, 1),
            });
        Db.SaveChanges();
        return p;
    }

    public Game AddGame(int id, int homeId, int awayId, DateTime date, GameStatus status = GameStatus.Scheduled,
        int? homeScore = null, int? awayScore = null)
    {
        var g = new Game
        {
            Id = id, Season = "2025-26", GameDate = date, Status = status,
            HomeTeamId = homeId, AwayTeamId = awayId, HomeScore = homeScore, AwayScore = awayScore,
        };
        Db.Games.Add(g);
        Db.SaveChanges();
        return g;
    }

    private int _tick;

    public void AddEvent(int gameId, int teamId, int playerId, byte points)
    {
        Db.ScoringEvents.Add(new ScoringEvent
        {
            GameId = gameId, TeamId = teamId, PlayerId = playerId, Points = points,
            CreatedAt = new DateTime(2026, 4, 1, 12, 0, 0).AddSeconds(_tick++),
        });
        Db.SaveChanges();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

// Simulates another scorer winning the race: the next SaveChanges fails with a concurrency conflict.
public sealed class ThrowOnceInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
{
    public bool Armed { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!Armed) return base.SavingChangesAsync(eventData, result, cancellationToken);
        Armed = false;
        throw new DbUpdateConcurrencyException("simulated conflict");
    }
}
