using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses;

/// <summary>
/// Marker interface for all API response types — provides compile-time access to Success and Error.
/// </summary>
public interface IApiResponse
{
    bool Success { get; }

    ApiError? Error { get; }
}

public class ApiResponseBase<T> : IApiResponse where T : class, new()
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public ApiError? Error { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

public class ApiError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("errors")]
    public ApiErrors? Errors { get; init; }
}

public class ApiErrors
{
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("index")]
    public int[]? Index { get; init; }
}
