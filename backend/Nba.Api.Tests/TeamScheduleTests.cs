using Nba.Api.Models;
using Nba.Api.Services;

namespace Nba.Api.Tests;

public class TeamScheduleTests
{
    [Fact]
    public async Task Schedule_is_in_date_order_with_opponent_and_home_away()
    {
        using var t = new TestDb();
        t.AddGame(10, homeId: 1, awayId: 2, new DateTime(2026, 4, 5, 19, 0, 0));
        t.AddGame(11, homeId: 2, awayId: 1, new DateTime(2026, 4, 1, 19, 0, 0));
        t.AddGame(12, homeId: 2, awayId: 3, new DateTime(2026, 4, 3, 19, 0, 0)); // not a Lakers game

        var result = await new TeamService(t.Db).GetScheduleAsync(1, null, default);

        var games = result.Value!.ToList();
        Assert.Equal(new[] { 11, 10 }, games.Select(g => g.Id));
        Assert.False(games[0].IsHome);
        Assert.Equal("Celtics", games[0].OpponentName);
        Assert.True(games[1].IsHome);
        Assert.Equal(2, games[1].OpponentId);
    }

    [Fact]
    public async Task Scores_and_result_are_from_the_requested_teams_perspective()
    {
        using var t = new TestDb();
        // Lakers (home) beat Celtics 112-105; Celtics view is the mirror image.
        t.AddGame(10, 1, 2, new DateTime(2026, 4, 1), GameStatus.Final, homeScore: 112, awayScore: 105);

        var lakers = (await new TeamService(t.Db).GetScheduleAsync(1, null, default)).Value!.Single();
        var celtics = (await new TeamService(t.Db).GetScheduleAsync(2, null, default)).Value!.Single();

        Assert.Equal((112, 105, "W"), (lakers.TeamScore, lakers.OpponentScore, lakers.Result));
        Assert.Equal((105, 112, "L"), (celtics.TeamScore, celtics.OpponentScore, celtics.Result));
    }

    [Fact]
    public async Task Final_game_without_scores_has_no_result()
    {
        using var t = new TestDb();
        t.AddGame(10, 1, 2, new DateTime(2026, 4, 1), GameStatus.Final);

        var game = (await new TeamService(t.Db).GetScheduleAsync(1, null, default)).Value!.Single();

        Assert.Null(game.Result);
        Assert.Null(game.TeamScore);
    }

    [Fact]
    public async Task Status_filter_is_applied()
    {
        using var t = new TestDb();
        t.AddGame(10, 1, 2, new DateTime(2026, 4, 1), GameStatus.Final);
        t.AddGame(11, 1, 3, new DateTime(2026, 4, 9), GameStatus.Scheduled);

        var upcoming = (await new TeamService(t.Db).GetScheduleAsync(1, GameStatus.Scheduled, default)).Value!.ToList();

        Assert.Equal(11, Assert.Single(upcoming).Id);
    }

    [Fact]
    public async Task Unknown_team_returns_404()
    {
        using var t = new TestDb();

        var result = await new TeamService(t.Db).GetScheduleAsync(999, null, default);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }
}
