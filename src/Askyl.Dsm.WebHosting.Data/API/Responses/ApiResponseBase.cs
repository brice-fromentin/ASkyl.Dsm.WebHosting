using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class ApiResponseBase<T> where T : class, new()
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