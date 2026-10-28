using Nba.Api.Models;
using Nba.Api.Services;

namespace Nba.Api.Tests;

public class ScorecardServiceTests
{
    private static readonly DateTime Date = new(2026, 4, 1, 19, 30, 0);

    [Fact]
    public async Task Unknown_game_returns_null()
    {
        using var t = new TestDb();
        Assert.Null(await new ScorecardService(t.Db).BuildAsync(999, default));
    }

    [Fact]
    public async Task Game_without_events_lists_active_rosters_with_zero_totals()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", teamId: 1);
        t.AddPlayer(2, "Jayson", "Tatum", teamId: 2);
        t.AddPlayer(3, "Old", "Timer", teamId: 1, active: false);   // left the team
        t.AddPlayer(4, "Zach", "LaVine", teamId: 3);                // on neither team
        t.AddGame(10, homeId: 2, awayId: 1, Date);

        var sc = (await new ScorecardService(t.Db).BuildAsync(10, default))!;

        Assert.Equal(0, sc.EventCount);
        Assert.Equal("Lakers", sc.AwayTeam.Name);
        Assert.Equal("Celtics", sc.HomeTeam.Name);
        Assert.Equal(new[] { "LeBron James" }, sc.AwayTeam.Players.Select(p => p.Name));
        Assert.Equal(new[] { "Jayson Tatum" }, sc.HomeTeam.Players.Select(p => p.Name));
        Assert.All(sc.AwayTeam.Players.Concat(sc.HomeTeam.Players), p => Assert.Equal((0, 0, 0), (p.Points, p.TwoPointers, p.ThreePointers)));
        Assert.Equal(0, sc.AwayTeam.TotalPoints);
    }

    [Fact]
    public async Task Totals_are_aggregated_per_player_and_team()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.AddPlayer(2, "Austin", "Reaves", 1);
        t.AddPlayer(3, "Jayson", "Tatum", 2);
        t.AddGame(10, homeId: 2, awayId: 1, Date, GameStatus.InProgress);
        t.AddEvent(10, 1, 1, 3);
        t.AddEvent(10, 1, 1, 2);
        t.AddEvent(10, 1, 1, 3);
        t.AddEvent(10, 1, 2, 2);
        t.AddEvent(10, 2, 3, 3);

        var sc = (await new ScorecardService(t.Db).BuildAsync(10, default))!;

        var lebron = sc.AwayTeam.Players.Single(p => p.PlayerId == 1);
        Assert.Equal((8, 1, 2), (lebron.Points, lebron.TwoPointers, lebron.ThreePointers));
        Assert.Equal(10, sc.AwayTeam.TotalPoints);
        Assert.Equal(3, sc.HomeTeam.TotalPoints);
        Assert.Equal(5, sc.EventCount);
        Assert.Equal(GameStatus.InProgress, sc.Status);
    }

    [Fact]
    public async Task Events_from_other_games_are_ignored()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.AddGame(10, 2, 1, Date);
        t.AddGame(11, 3, 1, Date.AddDays(2));
        t.AddEvent(11, 1, 1, 3);

        var sc = (await new ScorecardService(t.Db).BuildAsync(10, default))!;

        Assert.Equal(0, sc.AwayTeam.TotalPoints);
        Assert.Equal(0, sc.EventCount);
    }

    [Fact]
    public async Task Player_who_scored_but_left_the_roster_is_still_listed()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "Old", "Timer", teamId: 1, active: false);
        t.AddGame(10, 2, 1, Date);
        t.AddEvent(10, 1, 1, 2);

        var sc = (await new ScorecardService(t.Db).BuildAsync(10, default))!;

        var p = Assert.Single(sc.AwayTeam.Players);
        Assert.Equal("Old Timer", p.Name);
        Assert.Equal(2, p.Points);
        Assert.Equal(2, sc.AwayTeam.TotalPoints);
    }

    [Fact]
    public async Task Players_are_sorted_by_last_name()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "Zed", "Brown", 1);
        t.AddPlayer(2, "Amy", "Young", 1);
        t.AddPlayer(3, "Bo", "Adams", 1);
        t.AddGame(10, 2, 1, Date);

        var sc = (await new ScorecardService(t.Db).BuildAsync(10, default))!;

        Assert.Equal(new[] { "Bo Adams", "Zed Brown", "Amy Young" }, sc.AwayTeam.Players.Select(p => p.Name));
    }
}
