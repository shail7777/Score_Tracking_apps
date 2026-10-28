using Microsoft.AspNetCore.Mvc;
using Nba.Api.Controllers;
using Nba.Api.Dtos;
using Nba.Api.Models;
using Nba.Api.Services;

namespace Nba.Api.Tests;

public class ScoringServiceTests
{
    private static readonly DateTime FutureDate = DateTime.UtcNow.AddDays(10);
    private static readonly DateTime PastDate = new(2026, 4, 1, 19, 30, 0, DateTimeKind.Utc);

    // Lakers (team 1) are away at the Celtics (team 2); LeBron (1) and Reaves (2) play for LA, Tatum (3) for Boston.
    private static (TestDb Db, ScoringService Svc) Setup(DateTime? date = null, GameStatus status = GameStatus.Scheduled)
    {
        var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.AddPlayer(2, "Austin", "Reaves", 1);
        t.AddPlayer(3, "Jayson", "Tatum", 2);
        t.AddPlayer(4, "Zach", "LaVine", 3); // plays for neither team
        t.AddGame(10, homeId: 2, awayId: 1, date ?? FutureDate, status);
        return (t, new ScoringService(t.Db, new ScorecardService(t.Db)));
    }

    private static ScorecardPlayerDto Player(ScorecardDto sc, int id) =>
        sc.AwayTeam.Players.Concat(sc.HomeTeam.Players).Single(p => p.PlayerId == id);

    [Fact]
    public async Task Recording_a_three_and_a_two_updates_player_team_and_game_totals()
    {
        var (t, svc) = Setup();
        using var _ = t;

        await svc.RecordAsync(10, 1, 3, default);
        var result = await svc.RecordAsync(10, 1, 2, default);

        var sc = result.Scorecard!;
        Assert.Equal(200, result.StatusCode);
        Assert.Equal((5, 1, 1), (Player(sc, 1).Points, Player(sc, 1).TwoPointers, Player(sc, 1).ThreePointers));
        Assert.Equal(5, sc.AwayTeam.TotalPoints);
        Assert.Equal(0, sc.HomeTeam.TotalPoints);
        Assert.Equal(2, sc.EventCount);
        Assert.Equal(GameStatus.InProgress, sc.Status);

        var game = t.Db.Games.Single();
        Assert.Equal((0, 5), (game.HomeScore, game.AwayScore));
    }

    [Fact]
    public async Task Home_team_points_go_to_the_home_score()
    {
        var (t, svc) = Setup();
        using var _ = t;

        await svc.RecordAsync(10, 3, 3, default);

        var game = t.Db.Games.Single();
        Assert.Equal((3, 0), (game.HomeScore, game.AwayScore));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Points_other_than_two_or_three_are_rejected(int points)
    {
        var (t, svc) = Setup();
        using var _ = t;

        var result = await svc.RecordAsync(10, 1, points, default);

        Assert.Equal(400, result.StatusCode);
        Assert.Empty(t.Db.ScoringEvents);
    }

    [Fact]
    public async Task Player_not_on_either_roster_is_rejected()
    {
        var (t, svc) = Setup();
        using var _ = t;

        var result = await svc.RecordAsync(10, playerId: 4, points: 2, default);

        Assert.Equal(400, result.StatusCode);
        Assert.Contains("roster", result.Error);
        Assert.Empty(t.Db.ScoringEvents);
    }

    [Fact]
    public async Task Unknown_game_or_player_is_rejected()
    {
        var (t, svc) = Setup();
        using var _ = t;

        Assert.Equal(404, (await svc.RecordAsync(999, 1, 2, default)).StatusCode);
        Assert.Equal(400, (await svc.RecordAsync(10, 999, 2, default)).StatusCode);
    }

    [Fact]
    public async Task Past_imported_game_with_no_events_can_be_scored()
    {
        var (t, svc) = Setup(PastDate, GameStatus.Final);
        using var _ = t;

        var result = await svc.RecordAsync(10, 1, 2, default);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(GameStatus.InProgress, result.Scorecard!.Status);
        Assert.False(result.Scorecard.IsLocked);
    }

    [Fact]
    public async Task Undo_removes_the_latest_event_and_restores_totals()
    {
        var (t, svc) = Setup();
        using var _ = t;
        await svc.RecordAsync(10, 1, 3, default);
        await svc.RecordAsync(10, 3, 2, default);
        await svc.RecordAsync(10, 1, 2, default);

        var result = await svc.UndoLastAsync(10, default);

        var sc = result.Scorecard!;
        Assert.Equal(3, sc.AwayTeam.TotalPoints);
        Assert.Equal(2, sc.HomeTeam.TotalPoints);
        Assert.Equal(2, sc.EventCount);
        var game = t.Db.Games.Single();
        Assert.Equal((2, 3), (game.HomeScore, game.AwayScore));
    }

    [Fact]
    public async Task Undoing_the_only_event_returns_to_the_unscored_state()
    {
        var (t, svc) = Setup();
        using var _ = t;
        await svc.RecordAsync(10, 1, 3, default);

        var result = await svc.UndoLastAsync(10, default);

        Assert.Equal(GameStatus.Scheduled, result.Scorecard!.Status);
        var game = t.Db.Games.Single();
        Assert.Null(game.HomeScore);
        Assert.Null(game.AwayScore);
    }

    [Fact]
    public async Task Undoing_the_only_event_of_a_past_game_makes_it_final_again()
    {
        var (t, svc) = Setup(PastDate, GameStatus.Final);
        using var _ = t;
        await svc.RecordAsync(10, 1, 3, default);

        var result = await svc.UndoLastAsync(10, default);

        Assert.Equal(GameStatus.Final, result.Scorecard!.Status);
        Assert.False(result.Scorecard.IsLocked);
    }

    [Fact]
    public async Task Undo_with_no_events_is_rejected()
    {
        var (t, svc) = Setup();
        using var _ = t;

        Assert.Equal(400, (await svc.UndoLastAsync(10, default)).StatusCode);
        Assert.Equal(404, (await svc.UndoLastAsync(999, default)).StatusCode);
    }

    [Fact]
    public async Task Finish_requires_at_least_one_event()
    {
        var (t, svc) = Setup();
        using var _ = t;

        var result = await svc.FinishAsync(10, default);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(GameStatus.Scheduled, t.Db.Games.Single().Status);
    }

    [Fact]
    public async Task Finish_rejects_a_tied_score()
    {
        var (t, svc) = Setup();
        using var _ = t;
        await svc.RecordAsync(10, 1, 2, default);
        await svc.RecordAsync(10, 3, 2, default);

        var result = await svc.FinishAsync(10, default);

        Assert.Equal(400, result.StatusCode);
        Assert.Contains("tie", result.Error);
    }

    [Fact]
    public async Task Finish_locks_the_game_against_further_scoring_and_undo()
    {
        var (t, svc) = Setup();
        using var _ = t;
        await svc.RecordAsync(10, 1, 3, default);

        var finished = await svc.FinishAsync(10, default);

        Assert.Equal(GameStatus.Final, finished.Scorecard!.Status);
        Assert.True(finished.Scorecard.IsLocked);
        Assert.Equal(400, (await svc.RecordAsync(10, 1, 2, default)).StatusCode);
        Assert.Equal(400, (await svc.UndoLastAsync(10, default)).StatusCode);
        Assert.Equal(400, (await svc.FinishAsync(10, default)).StatusCode);
        Assert.Single(t.Db.ScoringEvents);
    }

    [Fact]
    public async Task Concurrent_update_returns_409_and_saves_nothing()
    {
        var (t, svc) = Setup();
        using var _ = t;
        t.Conflict.Armed = true;

        var result = await svc.RecordAsync(10, 1, 3, default);

        Assert.Equal(409, result.StatusCode);
        Assert.Empty(t.Db.ScoringEvents);
    }

    [Fact]
    public async Task Controller_maps_outcomes_to_http_results()
    {
        var (t, svc) = Setup();
        using var _ = t;
        var controller = new GameScoringController(svc);

        var ok = await controller.Record(10, new RecordEventRequest(1, 3), default);
        var notFound = await controller.Record(999, new RecordEventRequest(1, 3), default);
        var badRequest = await controller.Record(10, new RecordEventRequest(4, 3), default);
        t.Conflict.Armed = true;
        var conflict = await controller.Record(10, new RecordEventRequest(1, 2), default);

        Assert.Equal(3, ok.Value!.AwayTeam.TotalPoints);
        Assert.IsType<NotFoundObjectResult>(notFound.Result);
        Assert.IsType<BadRequestObjectResult>(badRequest.Result);
        Assert.IsType<ConflictObjectResult>(conflict.Result);
    }
}
