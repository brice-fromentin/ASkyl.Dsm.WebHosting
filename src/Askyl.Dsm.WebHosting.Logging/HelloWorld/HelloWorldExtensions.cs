using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging.HelloWorld;

public static partial class HelloWorldExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Dice roll: {Die1} and {Die2}, sum: {Sum}")]
    public static partial void LogDiceRoll(this ILogger logger, int die1, int die2, int sum);
}
