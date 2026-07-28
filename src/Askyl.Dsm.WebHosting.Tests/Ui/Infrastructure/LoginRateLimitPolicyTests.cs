using System.Net;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Ui.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Infrastructure;

public class LoginRateLimitPolicyTests
{
    static DefaultHttpContext CreateContext(string? clientAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = clientAddress is null ? null : IPAddress.Parse(clientAddress);

        return context;
    }

    [Fact]
    public void Partition_SeparatesDistinctClients()
    {
        // The defect this guards: a non-partitioned fixed window gave every caller the same bucket,
        // so one anonymous client could exhaust the login allowance for every user.
        var first = LoginRateLimitPolicy.Partition(CreateContext("192.0.2.10"));
        var second = LoginRateLimitPolicy.Partition(CreateContext("192.0.2.11"));

        Assert.NotEqual(first.PartitionKey, second.PartitionKey);
    }

    [Fact]
    public void Partition_ReusesTheSameKeyForOneClient()
    {
        // Repeat attempts from one caller must share a bucket, or the limit never triggers.
        var first = LoginRateLimitPolicy.Partition(CreateContext("192.0.2.10"));
        var second = LoginRateLimitPolicy.Partition(CreateContext("192.0.2.10"));

        Assert.Equal(first.PartitionKey, second.PartitionKey);
    }

    [Fact]
    public void ResolvePartitionKey_FallsBackToASharedKey_WhenAddressIsUnknown()
    {
        // Throttled together rather than left unlimited.
        var key = LoginRateLimitPolicy.ResolvePartitionKey(CreateContext(null));

        Assert.Equal(ApplicationConstants.RateLimitUnknownPartitionKey, key);
    }

    [Fact]
    public void ResolvePartitionKey_UsesTheClientAddress()
    {
        var key = LoginRateLimitPolicy.ResolvePartitionKey(CreateContext("192.0.2.10"));

        Assert.Equal("192.0.2.10", key);
    }
}
