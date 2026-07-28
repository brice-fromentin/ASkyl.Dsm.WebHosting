using System.Threading.RateLimiting;
using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Ui.Infrastructure;

/// <summary>
/// Partitioning for the login throttle. Each client address gets its own window, so a single caller
/// can no longer exhaust the allowance for everyone — which is what a non-partitioned fixed window did.
/// </summary>
public static class LoginRateLimitPolicy
{
    /// <summary>
    /// Builds the rate limit partition for a request, keyed on the client address.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <returns>A fixed window partition scoped to the caller.</returns>
    public static RateLimitPartition<string> Partition(HttpContext context)
    {
        return RateLimitPartition.GetFixedWindowLimiter(ResolvePartitionKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = ApplicationConstants.LoginRateLimitPermitLimit,
            Window = TimeSpan.FromMinutes(ApplicationConstants.LoginRateLimitWindowMinutes)
        });
    }

    /// <summary>
    /// Resolves the partition key for a request. Requires UseForwardedHeaders to run first, otherwise
    /// every request proxied by DSM's nginx reports the loopback address and shares one partition.
    /// Keying on the exact address means a caller controlling many addresses — an IPv6 prefix, for
    /// instance — still gets one allowance each; blocking that needs prefix grouping upstream.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <returns>The client address, or a shared fallback when it is unavailable.</returns>
    public static string ResolvePartitionKey(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? ApplicationConstants.RateLimitUnknownPartitionKey;
}
