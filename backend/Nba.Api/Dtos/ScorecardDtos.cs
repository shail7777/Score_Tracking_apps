using System.ComponentModel.DataAnnotations;

namespace Nba.Api.Dtos;

// Quarter and clock are optional; when omitted the game's saved clock state is used.
public record RecordEventRequest(
    int PlayerId,
    [Range(2, 3)] int Points,
    [Range(1, 10)] int? Quarter = null,
    [Range(0, 720)] int? ClockSeconds = null);

public record SetClockRequest(
    [Range(1, 10)] int Quarter,
    [Range(0, 720)] int ClockSeconds);

public record ScorecardPlayerDto(int PlayerId, string Name, int Points, int TwoPointers, int ThreePointers);

public record ScorecardTeamDto(int TeamId, string City, string Name, int TotalPoints, List<ScorecardPlayerDto> Players);

// One scoring play, with the score immediately after it.
public record ScorecardEventDto(
    int Id,
    int Quarter,
    int? ClockSeconds,
    int TeamId,
    string TeamName,
    int PlayerId,
    string PlayerName,
    int Points,
    int AwayScore,
    int HomeScore,
    DateTime CreatedAt);

public record ScorecardDto(
    int GameId,
    string Season,
    DateTime GameDate,
    string? Arena,
    Models.GameStatus Status,
    int EventCount,
    bool IsLocked,
    int Quarter,
    int ClockSeconds,
    ScorecardTeamDto AwayTeam,
    ScorecardTeamDto HomeTeam,
    List<ScorecardEventDto> Events);
