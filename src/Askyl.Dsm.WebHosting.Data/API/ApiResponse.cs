using System;
using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API;

public class ApiResponse<T> where T : class, new()
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
}