using Nba.Api.Dtos;
using Nba.Api.Models;
using Nba.Api.Services;

namespace Nba.Api.Tests;

public class TeamServiceTests
{
    [Fact]
    public async Task GetAll_is_sorted_by_name()
    {
        using var t = new TestDb(); // Lakers, Celtics, Bulls

        var teams = await new TeamService(t.Db).GetAllAsync(default);

        Assert.Equal(new[] { "Bulls", "Celtics", "Lakers" }, teams.Select(x => x.Name));
    }

    [Fact]
    public async Task Get_returns_the_team_or_not_found()
    {
        using var t = new TestDb();
        var svc = new TeamService(t.Db);

        Assert.Equal("Celtics", (await svc.GetAsync(2, default)).Value!.Name);
        Assert.Equal(ServiceStatus.NotFound, (await svc.GetAsync(999, default)).Status);
    }

    [Fact]
    public async Task Create_saves_the_team_and_returns_created()
    {
        using var t = new TestDb();

        var result = await new TeamService(t.Db).CreateAsync(new TeamRequest("Bucks", "Milwaukee", "East", "Central"), default);

        Assert.Equal(ServiceStatus.Created, result.Status);
        Assert.Equal("Milwaukee", result.Value!.City);
        Assert.Equal(1, t.Db.Teams.Count(x => x.Name == "Bucks"));
    }

    [Fact]
    public async Task Update_changes_the_team_and_unknown_id_is_not_found()
    {
        using var t = new TestDb();
        var svc = new TeamService(t.Db);

        var ok = await svc.UpdateAsync(1, new TeamRequest("Lakers", "LA", "West", "Pacific"), default);
        var missing = await svc.UpdateAsync(999, new TeamRequest("X", "Y", "West", "Z"), default);

        Assert.True(ok.Succeeded);
        Assert.Equal("LA", t.Db.Teams.Single(x => x.Id == 1).City);
        Assert.Equal(ServiceStatus.NotFound, missing.Status);
    }

    [Fact]
    public async Task Delete_is_blocked_when_the_team_has_games()
    {
        using var t = new TestDb();
        t.AddGame(10, 1, 2, new DateTime(2026, 4, 1));
        var svc = new TeamService(t.Db);

        var blocked = await svc.DeleteAsync(1, default);
        var removed = await svc.DeleteAsync(3, default);
        var missing = await svc.DeleteAsync(999, default);

        Assert.Equal(ServiceStatus.Conflict, blocked.Status);
        Assert.Contains("games", blocked.Error);
        Assert.True(removed.Succeeded);
        Assert.DoesNotContain(t.Db.Teams, x => x.Id == 3);
        Assert.Equal(ServiceStatus.NotFound, missing.Status);
    }

    [Fact]
    public async Task Roster_lists_active_players_by_default_and_former_players_on_request()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "Zed", "Brown", 1);
        t.AddPlayer(2, "Amy", "Adams", 1);
        t.AddPlayer(3, "Old", "Timer", 1, active: false);
        var svc = new TeamService(t.Db);

        var active = (await svc.GetRosterAsync(1, true, default)).Value!;
        var all = (await svc.GetRosterAsync(1, false, default)).Value!;

        Assert.Equal(new[] { "Amy Adams", "Zed Brown" }, active.Select(x => x.PlayerName));
        Assert.Equal(3, all.Count);
        Assert.Equal(ServiceStatus.NotFound, (await svc.GetRosterAsync(999, true, default)).Status);
    }
}

public class PlayerServiceTests
{
    [Fact]
    public async Task GetAll_filters_by_current_team_and_name_and_shows_the_current_team()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.AddPlayer(2, "Jayson", "Tatum", 2);
        t.AddPlayer(3, "Free", "Agent", null);
        var svc = new PlayerService(t.Db);

        var all = await svc.GetAllAsync(null, null, default);
        var lakers = await svc.GetAllAsync(1, null, default);
        var search = await svc.GetAllAsync(null, "Tat", default);

        Assert.Equal(new[] { "Agent", "James", "Tatum" }, all.Select(p => p.LastName));
        Assert.Null(all.Single(p => p.LastName == "Agent").CurrentTeamName);
        Assert.Equal("LeBron", Assert.Single(lakers).FirstName);
        Assert.Equal("Celtics", Assert.Single(search).CurrentTeamName);
    }

    [Fact]
    public async Task Former_team_is_not_the_current_team()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "Old", "Timer", 1, active: false);

        var player = (await new PlayerService(t.Db).GetAsync(1, default)).Value!;

        Assert.Null(player.CurrentTeamId);
        Assert.Empty(await new PlayerService(t.Db).GetAllAsync(1, null, default));
    }

    [Fact]
    public async Task History_lists_every_stint_newest_first()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.Db.TeamPlayers.Add(new TeamPlayer { PlayerId = 1, TeamId = 2, StartDate = new DateOnly(2020, 1, 1), EndDate = new DateOnly(2025, 1, 1) });
        t.Db.SaveChanges();

        var history = (await new PlayerService(t.Db).GetHistoryAsync(1, default)).Value!;

        Assert.Equal(new[] { "Lakers", "Celtics" }, history.Select(h => h.TeamName));
        Assert.Equal(ServiceStatus.NotFound, (await new PlayerService(t.Db).GetHistoryAsync(999, default)).Status);
    }

    [Fact]
    public async Task Create_and_update_round_trip()
    {
        using var t = new TestDb();
        var svc = new PlayerService(t.Db);

        var created = await svc.CreateAsync(new PlayerRequest("New", "Guy", null, null, null), default);
        var updated = await svc.UpdateAsync(created.Value!.Id, new PlayerRequest("New", "Fellow", 80, 200, null), default);

        Assert.Equal(ServiceStatus.Created, created.Status);
        Assert.True(updated.Succeeded);
        Assert.Equal("Fellow", t.Db.Players.Single().LastName);
        Assert.Equal(ServiceStatus.NotFound, (await svc.UpdateAsync(999, new PlayerRequest("A", "B", null, null, null), default)).Status);
    }

    [Fact]
    public async Task Delete_is_blocked_when_the_player_has_scoring_events()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.AddPlayer(2, "Bench", "Warmer", 1);
        t.AddGame(10, 2, 1, new DateTime(2026, 4, 1));
        t.AddEvent(10, 1, 1, 3);
        var svc = new PlayerService(t.Db);

        var blocked = await svc.DeleteAsync(1, default);
        var removed = await svc.DeleteAsync(2, default);

        Assert.Equal(ServiceStatus.Conflict, blocked.Status);
        Assert.True(removed.Succeeded);
        Assert.Equal(1, t.Db.Players.Count());
    }
}

public class TeamPlayerServiceTests
{
    private static TeamPlayerRequest Req(int teamId, int playerId, DateOnly start, DateOnly? end = null) =>
        new(teamId, playerId, start, end);

    [Fact]
    public async Task Adding_an_active_stint_ends_the_players_previous_active_stint()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);

        var result = await new TeamPlayerService(t.Db).CreateAsync(Req(2, 1, new DateOnly(2026, 10, 27)), default);

        Assert.Equal(ServiceStatus.Created, result.Status);
        Assert.True(result.Value!.IsActive);
        var old = t.Db.TeamPlayers.Single(tp => tp.TeamId == 1);
        Assert.Equal(new DateOnly(2026, 10, 27), old.EndDate);
        Assert.Equal(1, t.Db.TeamPlayers.Count(tp => tp.EndDate == null));
    }

    [Fact]
    public async Task Adding_a_finished_stint_leaves_the_current_one_alone()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);

        await new TeamPlayerService(t.Db).CreateAsync(Req(2, 1, new DateOnly(2020, 1, 1), new DateOnly(2021, 1, 1)), default);

        Assert.Null(t.Db.TeamPlayers.Single(tp => tp.TeamId == 1).EndDate);
    }

    [Theory]
    [InlineData(999, 1, "Team 999 does not exist.")]
    [InlineData(1, 999, "Player 999 does not exist.")]
    public async Task Unknown_team_or_player_is_a_bad_request(int teamId, int playerId, string message)
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);

        var result = await new TeamPlayerService(t.Db).CreateAsync(Req(teamId, playerId, new DateOnly(2026, 1, 1)), default);

        Assert.Equal(ServiceStatus.BadRequest, result.Status);
        Assert.Equal(message, result.Error);
    }

    [Fact]
    public async Task End_date_before_start_date_is_rejected_on_create_and_update()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        var svc = new TeamPlayerService(t.Db);
        var bad = Req(1, 1, new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1));

        Assert.Equal(ServiceStatus.BadRequest, (await svc.CreateAsync(bad, default)).Status);
        Assert.Equal(ServiceStatus.BadRequest, (await svc.UpdateAsync(t.Db.TeamPlayers.Single().Id, bad, default)).Status);
    }

    [Fact]
    public async Task Get_update_delete_report_not_found_for_unknown_ids()
    {
        using var t = new TestDb();
        var svc = new TeamPlayerService(t.Db);

        Assert.Equal(ServiceStatus.NotFound, (await svc.GetAsync(999, default)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await svc.UpdateAsync(999, Req(1, 1, new DateOnly(2026, 1, 1)), default)).Status);
        Assert.Equal(ServiceStatus.NotFound, (await svc.DeleteAsync(999, default)).Status);
    }

    [Fact]
    public async Task GetAll_filters_by_team_player_and_active()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.AddPlayer(2, "Old", "Timer", 1, active: false);
        var svc = new TeamPlayerService(t.Db);

        Assert.Equal(2, (await svc.GetAllAsync(1, null, false, default)).Count);
        Assert.Single(await svc.GetAllAsync(1, null, true, default));
        Assert.Single(await svc.GetAllAsync(null, 2, false, default));
    }
}

public class GameServiceTests
{
    private static readonly DateTime D = new(2026, 4, 1, 19, 30, 0);

    private static GameRequest Req(int home = 2, int away = 1, GameStatus status = GameStatus.Scheduled,
        int? homeScore = null, int? awayScore = null) =>
        new("2025-26", D, "TD Garden", status, home, away, homeScore, awayScore);

    [Theory]
    [InlineData(1, 1, GameStatus.Scheduled, null, null, "Home and away teams must be different.")]
    [InlineData(2, 99, GameStatus.Scheduled, null, null, "Home or away team does not exist.")]
    [InlineData(2, 1, GameStatus.Final, 100, null, "Final games require both scores.")]
    [InlineData(2, 1, GameStatus.Final, 100, 100, "NBA games cannot end in a tie.")]
    [InlineData(2, 1, GameStatus.Scheduled, 90, 80, "Scheduled games cannot have scores.")]
    public async Task Create_rejects_invalid_games(int home, int away, GameStatus status, int? hs, int? aws, string message)
    {
        using var t = new TestDb();

        var result = await new GameService(t.Db).CreateAsync(Req(home, away, status, hs, aws), default);

        Assert.Equal(ServiceStatus.BadRequest, result.Status);
        Assert.Equal(message, result.Error);
        Assert.Empty(t.Db.Games);
    }

    [Fact]
    public async Task Create_saves_a_final_game_and_reports_the_winner_and_team_names()
    {
        using var t = new TestDb();

        var result = await new GameService(t.Db).CreateAsync(Req(2, 1, GameStatus.Final, 112, 105), default);

        var game = result.Value!;
        Assert.Equal(ServiceStatus.Created, result.Status);
        Assert.Equal(("Celtics", "Lakers", 2), (game.HomeTeamName, game.AwayTeamName, game.WinnerTeamId));
    }

    [Fact]
    public async Task Update_is_blocked_once_the_game_has_scoring_events()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);
        t.AddGame(10, 2, 1, D);
        t.AddEvent(10, 1, 1, 3);

        var result = await new GameService(t.Db).UpdateAsync(10, Req(), default);

        Assert.Equal(ServiceStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Update_and_delete_work_and_unknown_ids_are_not_found()
    {
        using var t = new TestDb();
        t.AddGame(10, 2, 1, D);
        var svc = new GameService(t.Db);

        Assert.True((await svc.UpdateAsync(10, Req(2, 1, GameStatus.Final, 100, 90), default)).Succeeded);
        Assert.Equal(100, t.Db.Games.Single().HomeScore);
        Assert.Equal(ServiceStatus.NotFound, (await svc.UpdateAsync(999, Req(), default)).Status);
        Assert.True((await svc.DeleteAsync(10, default)).Succeeded);
        Assert.Empty(t.Db.Games);
        Assert.Equal(ServiceStatus.NotFound, (await svc.DeleteAsync(10, default)).Status);
    }

    [Fact]
    public async Task GetAll_orders_previous_games_newest_first_and_upcoming_soonest_first_and_filters()
    {
        using var t = new TestDb();
        t.AddGame(1, 2, 1, D, GameStatus.Final, 100, 90);
        t.AddGame(2, 2, 1, D.AddDays(2), GameStatus.Final, 90, 80);
        t.AddGame(3, 2, 3, D.AddDays(30), GameStatus.Scheduled);
        t.AddGame(4, 1, 3, D.AddDays(10), GameStatus.Scheduled);
        var svc = new GameService(t.Db);

        var previous = await svc.GetAllAsync(null, GameStatus.Final, null, default);
        var upcoming = await svc.GetAllAsync(null, GameStatus.Scheduled, null, default);
        var lakers = await svc.GetAllAsync(1, null, null, default);
        var otherSeason = await svc.GetAllAsync(null, null, "2024-25", default);

        Assert.Equal(new[] { 2, 1 }, previous.Select(g => g.Id));
        Assert.Equal(new[] { 4, 3 }, upcoming.Select(g => g.Id));
        Assert.Equal(new[] { 4, 2, 1 }, lakers.Select(g => g.Id));
        Assert.Empty(otherSeason);
    }

    [Fact]
    public async Task Get_returns_the_game_or_not_found()
    {
        using var t = new TestDb();
        t.AddGame(10, 2, 1, D);
        var svc = new GameService(t.Db);

        Assert.Equal("Celtics", (await svc.GetAsync(10, default)).Value!.HomeTeamName);
        Assert.Equal(ServiceStatus.NotFound, (await svc.GetAsync(999, default)).Status);
    }
}
