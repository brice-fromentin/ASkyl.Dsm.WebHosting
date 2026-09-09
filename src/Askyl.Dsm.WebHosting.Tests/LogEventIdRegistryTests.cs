using System.Reflection;
using Askyl.Dsm.WebHosting.Constants.Logging;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tests;

/// <summary>
/// Holds the assembly to what <see cref="LogEventIds"/> claims about it.
/// </summary>
/// <remarks>
/// The ranges used to be prose in a doc comment, kept in step by hand as §6.6 asks. That instruction failed
/// twice — once by declaring two ids deleted in PR #59, once by declaring one fewer than DsmSettingsService
/// had used for weeks — and neither format, build nor test could see it.
/// </remarks>
public class LogEventIdRegistryTests
{
    /// <summary>
    /// The declared ranges, discovered by pairing each <c>XxxBase</c> constant with its <c>XxxLast</c>.
    /// </summary>
    static IReadOnlyList<(string Owner, int Base, int Last)> DeclaredRanges()
    {
        var constants = typeof(LogEventIds)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(int))
            .ToDictionary(field => field.Name, field => (int)field.GetRawConstantValue()!);

        return [.. constants.Keys
            .Where(name => name.EndsWith("Base", StringComparison.Ordinal))
            .Select(name => name[..^"Base".Length])
            .Select(owner => (Owner: owner, Base: constants[owner + "Base"], Last: constants[owner + "Last"]))
            .OrderBy(range => range.Base)];
    }

    /// <summary>
    /// Every EventId the logging assembly actually declares, read from the attribute rather than the source.
    /// </summary>
    static IReadOnlyList<(string Method, int EventId)> DeclaredEventIds()
    {
        return [.. typeof(ILogDsmSession).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(method => (method.Name, Attribute: method.GetCustomAttribute<LoggerMessageAttribute>()))
            .Where(entry => entry.Attribute is not null)
            .Select(entry => (Method: entry.Name, entry.Attribute!.EventId))];
    }

    [Fact]
    public void TheRegistryAndTheAssembly_AreBothNonEmpty()
    {
        // The guard that keeps the rest of this class honest. Every assertion below discovers its own
        // inputs, so an implementation that silently found nothing would pass all of them. This is the
        // reason the first sketch of this test — parsing ranges out of doc comments — was thrown away:
        // one hyphen typed for an en dash and it would have verified nothing, in green.
        Assert.NotEmpty(DeclaredRanges());
        Assert.NotEmpty(DeclaredEventIds());
    }

    [Fact]
    public void NoTwoLogMethods_ShareAnEventId()
    {
        // compare-logs.sh groups a deployment log by owning service through these ids. Two services under
        // one id makes that grouping silently wrong.
        var duplicates = DeclaredEventIds()
            .GroupBy(entry => entry.EventId)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {String.Join(", ", group.Select(entry => entry.Method))}")
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void EveryEventId_FallsInsideExactlyOneDeclaredRange()
    {
        var ranges = DeclaredRanges();

        var orphans = DeclaredEventIds()
            .Where(entry => ranges.Count(range => entry.EventId > range.Base && entry.EventId <= range.Last) != 1)
            .Select(entry => $"{entry.EventId} ({entry.Method})")
            .ToList();

        // An id above its range's Last lands here — which is the shape of "a log method was added and the
        // range was not extended", the drift that went unnoticed in the 2800000 block.
        Assert.Empty(orphans);
    }

    [Fact]
    public void NoTwoRanges_Overlap()
    {
        var ranges = DeclaredRanges();

        var overlaps = ranges
            .SelectMany(left => ranges.Where(right => right.Owner != left.Owner
                                                      && right.Base <= left.Last
                                                      && left.Base <= right.Last)
                                      .Select(right => $"{left.Owner} and {right.Owner}"))
            .ToList();

        Assert.Empty(overlaps);
    }

    [Fact]
    public void EveryRange_EndsAtTheHighestIdItActuallyUses()
    {
        var ids = DeclaredEventIds();

        var wrong = DeclaredRanges()
            .Select(range => (range.Owner, range.Last, Used: ids.Where(entry => entry.EventId > range.Base && entry.EventId <= range.Last)
                                                                .Select(entry => entry.EventId)
                                                                .ToList()))
            .Where(range => range.Used.Count == 0 || range.Used.Max() != range.Last)
            .Select(range => range.Used.Count == 0
                ? $"{range.Owner}: declared through {range.Last}, uses nothing"
                : $"{range.Owner}: declared through {range.Last}, highest used is {range.Used.Max()}")
            .ToList();

        // The half that catches a deleted log method. Removing one leaves the range claiming ids that no
        // longer exist, which is exactly how PR #59 left the 1900000 block.
        Assert.Empty(wrong);
    }
}
