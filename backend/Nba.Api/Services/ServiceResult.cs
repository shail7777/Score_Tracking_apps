namespace Nba.Api.Services;

public enum ServiceStatus
{
    Ok,
    Created,
    NotFound,
    BadRequest,
    Conflict,
}

// Outcome of a service call. Services decide what happened; controllers translate it to HTTP.
public record ServiceResult(ServiceStatus Status, string? Error = null)
{
    public bool Succeeded => Status is ServiceStatus.Ok or ServiceStatus.Created;

    public static ServiceResult Ok() => new(ServiceStatus.Ok);
    public static ServiceResult NotFound() => new(ServiceStatus.NotFound);
    public static ServiceResult BadRequest(string error) => new(ServiceStatus.BadRequest, error);
    public static ServiceResult Conflict(string error) => new(ServiceStatus.Conflict, error);
}

public record ServiceResult<T>(ServiceStatus Status, T? Value = default, string? Error = null)
{
    public bool Succeeded => Status is ServiceStatus.Ok or ServiceStatus.Created;

    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Ok, value);
    public static ServiceResult<T> Created(T value) => new(ServiceStatus.Created, value);
    public static ServiceResult<T> NotFound() => new(ServiceStatus.NotFound);
    public static ServiceResult<T> BadRequest(string error) => new(ServiceStatus.BadRequest, default, error);
    public static ServiceResult<T> Conflict(string error) => new(ServiceStatus.Conflict, default, error);
}
