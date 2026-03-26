using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;

namespace Askyl.Dsm.WebHosting.Tools.Extensions;

/// <summary>
/// Extension methods for API response types that provide convenient validation helpers.
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Checks if the API response is not null and the operation was successful.
    /// Can optionally also check if the response contains data.
    /// </summary>
    public static bool IsValid<E>(this ApiResponseBase<E>? response, bool hasData = false) where E : class, new()
        => response is not null && response.Success && (!hasData || response.Data is not null);
}
