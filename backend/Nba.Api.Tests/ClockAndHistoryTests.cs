using Microsoft.AspNetCore.Mvc;
using Nba.Api.Controllers;
using Nba.Api.Dtos;
using Nba.Api.Models;
using Nba.Api.Services;

namespace Nba.Api.Tests;

public class ClockAndHistoryTests
{
    // Lakers (team 1) away at the Celtics (team 2); LeBron (1) and Reaves (2) play for LA, Tatum (3) for Boston.
    private static (TestDb Db, ScoringService Svc) Setup()
    {
        var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.AddPlayer(2, "Austin", "Reaves", 1);
        t.AddPlayer(3, "Jayson", "Tatum", 2);
        t.AddGame(10, homeId: 2, awayId: 1, DateTime.UtcNow.AddDays(10));
        return (t, new ScoringService(t.Db, new ScorecardService(t.Db)));
    }

    [Fact]
    public async Task New_game_starts_in_quarter_one_with_a_full_clock()
    {
        var (t, svc) = Setup();
        using var _ = t;

        var sc = (await new ScorecardService(t.Db).BuildAsync(10, default))!;

        Assert.Equal((1, 720), (sc.Quarter, sc.ClockSeconds));
        Assert.Empty(sc.Events);
    }

    [Fact]
    public async Task Basket_stores_the_quarter_and_clock_when_it_happened_and_saves_the_clock()
    {
        var (t, svc) = Setup();
        using var _ = t;

        var sc = (await svc.RecordAsync(10, 1, 3, default, quarter: 2, clockSeconds: 521)).Scorecard!;

        var play = Assert.Single(sc.Events);
        Assert.Equal((2, 521), (play.Quarter, play.ClockSeconds));
        Assert.Equal((2, 521), (sc.Quarter, sc.ClockSeconds));
        var saved = t.Db.ScoringEvents.Single();
        Assert.Equal((byte)2, saved.Quarter);
        Assert.Equal(521, saved.ClockSecondsRemaining);
    }

    [Fact]
    public async Task Basket_without_a_clock_uses_the_saved_clock()
    {
        var (t, svc) = Setup();
        using var _ = t;
        await svc.SetClockAsync(10, 3, 400, default);

        var sc = (await svc.RecordAsync(10, 1, 2, default)).Scorecard!;

        Assert.Equal((3, 400), (sc.Events[0].Quarter, sc.Events[0].ClockSeconds));
    }

    [Fact]
    public async Task History_is_newest_first_with_names_and_running_score()
    {
        var (t, svc) = Setup();
        using var _ = t;
        await svc.RecordAsync(10, 1, 3, default, 1, 600);   // LeBron +3  (away)
        await svc.RecordAsync(10, 3, 2, default, 1, 540);   // Tatum  +2  (home)
        await svc.RecordAsync(10, 2, 2, default, 1, 500);   // Reaves +2  (away)

        var events = (await new ScorecardService(t.Db).BuildAsync(10, default))!.Events;

        Assert.Equal(new[] { "Austin Reaves", "Jayson Tatum", "LeBron James" }, events.Select(e => e.PlayerName));
        Assert.Equal(new[] { "Lakers", "Celtics", "Lakers" }, events.Select(e => e.TeamName));
        Assert.Equal(new[] { (5, 2), (3, 2), (3, 0) }, events.Select(e => (e.AwayScore, e.HomeScore)));
        Assert.Equal(new[] { 2, 2, 3 }, events.Select(e => e.Points));
    }

    [Fact]
    public async Task Undo_removes_the_play_from_the_history()
    {
        var (t, svc) = Setup();
        using var _ = t;
        await svc.RecordAsync(10, 1, 3, default);
        await svc.RecordAsync(10, 3, 2, default);

        var sc = (await svc.UndoLastAsync(10, default)).Scorecard!;

        var play = Assert.Single(sc.Events);
        Assert.Equal("LeBron James", play.PlayerName);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(11, 100)]
    [InlineData(1, 721)]
    [InlineData(1, -1)]
    [InlineData(5, 301)]   // overtime periods are 5:00
    public async Task Invalid_quarter_or_clock_is_rejected(int quarter, int seconds)
    {
        var (t, svc) = Setup();
        using var _ = t;

        Assert.Equal(400, (await svc.RecordAsync(10, 1, 2, default, quarter, seconds)).StatusCode);
        Assert.Equal(400, (await svc.SetClockAsync(10, quarter, seconds, default)).StatusCode);
        Assert.Empty(t.Db.ScoringEvents);
    }

    [Fact]
    public async Task Overtime_allows_up_to_five_minutes()
    {
        var (t, svc) = Setup();
        using var _ = t;

        var result = await svc.SetClockAsync(10, 5, 300, default);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal((5, 300), (result.Scorecard!.Quarter, result.Scorecard.ClockSeconds));
    }

    [Fact]
    public async Task Set_clock_is_saved_and_does_not_change_the_game_status()
    {
        var (t, svc) = Setup();
        using var _ = t;

        await svc.SetClockAsync(10, 2, 345, default);

        var game = t.Db.Games.Single();
        Assert.Equal((2, 345, GameStatus.Scheduled), (game.Quarter, game.ClockSecondsRemaining, game.Status));
    }

    [Fact]
    public async Task Clock_cannot_change_once_the_game_is_final_with_events_and_unknown_game_is_404()
    {
        var (t, svc) = Setup();
        using var _ = t;
        await svc.RecordAsync(10, 1, 3, default);
        await svc.FinishAsync(10, default);

        Assert.Equal(400, (await svc.SetClockAsync(10, 2, 100, default)).StatusCode);
        Assert.Equal(404, (await svc.SetClockAsync(999, 1, 100, default)).StatusCode);
    }

    [Fact]
    public async Task Controller_maps_clock_outcomes()
    {
        var (t, svc) = Setup();
        using var _ = t;
        var controller = new GameScoringController(svc);

        var ok = await controller.SetClock(10, new SetClockRequest(2, 600), default);
        var bad = await controller.SetClock(10, new SetClockRequest(1, 9999), default);
        var missing = await controller.SetClock(999, new SetClockRequest(1, 600), default);

        Assert.Equal(2, ok.Value!.Quarter);
        Assert.IsType<BadRequestObjectResult>(bad.Result);
        Assert.IsType<NotFoundObjectResult>(missing.Result);
    }
}
