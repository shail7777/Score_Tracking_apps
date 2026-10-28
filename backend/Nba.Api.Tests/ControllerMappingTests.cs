using Microsoft.AspNetCore.Mvc;
using Nba.Api.Controllers;
using Nba.Api.Dtos;
using Nba.Api.Models;
using Nba.Api.Services;

namespace Nba.Api.Tests;

// The controllers only translate service results into HTTP responses.
public class ControllerMappingTests
{
    [Fact]
    public async Task Missing_resource_is_404()
    {
        using var t = new TestDb();

        var result = await new TeamsController(new TeamService(t.Db)).Get(999, default);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Found_resource_is_200_with_the_dto()
    {
        using var t = new TestDb();

        var result = await new TeamsController(new TeamService(t.Db)).Get(2, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Celtics", Assert.IsType<TeamDto>(ok.Value).Name);
    }

    [Fact]
    public async Task List_endpoints_return_200()
    {
        using var t = new TestDb();
        var controller = new TeamsController(new TeamService(t.Db));

        var teams = await controller.GetAll(default);
        var roster = await controller.GetRoster(1, true, default);
        var roster404 = await controller.GetRoster(999, true, default);

        Assert.Equal(3, Assert.IsAssignableFrom<IEnumerable<TeamDto>>(Assert.IsType<OkObjectResult>(teams.Result).Value).Count());
        Assert.IsType<OkObjectResult>(roster.Result);
        Assert.IsType<NotFoundResult>(roster404.Result);
    }

    [Fact]
    public async Task Create_is_201_with_a_location_for_the_new_resource()
    {
        using var t = new TestDb();

        var result = await new TeamsController(new TeamService(t.Db)).Create(new TeamRequest("Bucks", "Milwaukee", "East", "Central"), default);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(TeamsController.Get), created.ActionName);
        Assert.Equal(Assert.IsType<TeamDto>(created.Value).Id, created.RouteValues!["id"]);
    }

    [Fact]
    public async Task Update_and_delete_success_are_204()
    {
        using var t = new TestDb();
        var controller = new TeamsController(new TeamService(t.Db));

        Assert.IsType<NoContentResult>(await controller.Update(1, new TeamRequest("Lakers", "LA", "West", "Pacific"), default));
        Assert.IsType<NoContentResult>(await controller.Delete(3, default));
        Assert.IsType<NotFoundResult>(await controller.Delete(999, default));
    }

    [Fact]
    public async Task Conflict_is_409_with_the_message_as_the_problem_title()
    {
        using var t = new TestDb();
        t.AddGame(10, 1, 2, new DateTime(2026, 4, 1));

        var result = await new TeamsController(new TeamService(t.Db)).Delete(1, default);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Team has games and cannot be deleted.", Assert.IsType<ProblemDetails>(conflict.Value).Title);
    }

    [Fact]
    public async Task Invalid_request_is_400_with_the_message_as_the_problem_title()
    {
        using var t = new TestDb();
        var controller = new GamesController(new GameService(t.Db));

        var result = await controller.Create(new GameRequest("2025-26", DateTime.UtcNow, null, GameStatus.Scheduled, 1, 1, null, null), default);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Home and away teams must be different.", Assert.IsType<ProblemDetails>(bad.Value).Title);
    }

    [Fact]
    public async Task Players_and_team_players_controllers_use_their_services()
    {
        using var t = new TestDb();
        t.AddPlayer(1, "LeBron", "James", 1);

        var players = await new PlayersController(new PlayerService(t.Db)).GetAll(null, "Bron", default);
        var history = await new PlayersController(new PlayerService(t.Db)).GetHistory(999, default);
        var stints = await new TeamPlayersController(new TeamPlayerService(t.Db)).GetAll(1, null, true, default);
        var badStint = await new TeamPlayersController(new TeamPlayerService(t.Db))
            .Create(new TeamPlayerRequest(999, 1, new DateOnly(2026, 1, 1), null), default);

        Assert.Single(Assert.IsAssignableFrom<IEnumerable<PlayerDto>>(Assert.IsType<OkObjectResult>(players.Result).Value));
        Assert.IsType<NotFoundResult>(history.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<TeamPlayerDto>>(Assert.IsType<OkObjectResult>(stints.Result).Value));
        Assert.IsType<BadRequestObjectResult>(badStint.Result);
    }
}
