using System.ComponentModel.DataAnnotations;
using Nba.Api.Models;

namespace Nba.Api.Dtos;

public record TeamDto(int Id, string Name, string City, string Conference, string Division);

public record TeamRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(100)] string City,
    [Required, RegularExpression("East|West")] string Conference,
    [Required, StringLength(50)] string Division);

public record PlayerDto(
    int Id,
    string FirstName,
    string LastName,
    int? HeightInches,
    int? WeightLbs,
    DateOnly? BirthDate,
    int? CurrentTeamId,
    string? CurrentTeamName);

public record PlayerRequest(
    [Required, StringLength(50)] string FirstName,
    [Required, StringLength(50)] string LastName,
    [Range(48, 100)] int? HeightInches,
    [Range(100, 500)] int? WeightLbs,
    DateOnly? BirthDate);

public record TeamPlayerDto(
    int Id,
    int TeamId,
    string TeamName,
    int PlayerId,
    string PlayerName,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive);

public record TeamPlayerRequest(
    int TeamId,
    int PlayerId,
    DateOnly StartDate,
    DateOnly? EndDate);

public record GameDto(
    int Id,
    string Season,
    DateTime GameDate,
    string? Arena,
    GameStatus Status,
    int HomeTeamId,
    string HomeTeamName,
    int AwayTeamId,
    string AwayTeamName,
    int? HomeScore,
    int? AwayScore,
    int? WinnerTeamId);

// A game from one team's point of view (used by the team schedule).
public record TeamGameDto(
    int Id,
    string Season,
    DateTime GameDate,
    string? Arena,
    GameStatus Status,
    bool IsHome,
    int OpponentId,
    string OpponentName,
    int? TeamScore,
    int? OpponentScore,
    string? Result);

public record GameRequest(
    [Required, StringLength(7)] string Season,
    DateTime GameDate,
    [StringLength(100)] string? Arena,
    GameStatus Status,
    int HomeTeamId,
    int AwayTeamId,
    [Range(0, 300)] int? HomeScore,
    [Range(0, 300)] int? AwayScore);
