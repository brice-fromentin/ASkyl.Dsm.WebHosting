using Askyl.Dsm.WebHosting.Data.API.Responses;

namespace Askyl.Dsm.WebHosting.Tools.Extensions;

public static class ApiResponseExtensions
{
    /// <summary>
    /// Checks if the API response is not null and the operation was successful.
    /// Can optionally also check if the response contains data.
    /// </summary>
    /// <param name="response">The API response to validate.</param>
    /// <param name="hasData">If true, the method also checks that the Data property is not null.</param>
    public static bool IsValid<T>(this ApiResponseBase<T>? response, bool hasData = false) where T : class, new()
        => response is not null && response.Success && (!hasData || response.Data is not null);
}