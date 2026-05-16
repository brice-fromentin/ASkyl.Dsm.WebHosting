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
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public ApiError? Error { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

public class ApiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("errors")]
    public ApiErrors? Errors { get; set; }
}

public class ApiErrors
{
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("index")]
    public int[]? Index { get; set; }
}
