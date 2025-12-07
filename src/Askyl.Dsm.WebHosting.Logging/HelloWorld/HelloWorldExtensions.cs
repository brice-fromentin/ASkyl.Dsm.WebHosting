using System;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging.HelloWorld;

public static partial class HelloWorldExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Lancer de dé : {Die1} et {Die2}, somme : {Sum}")]
    public static partial void LogDiceRoll(this ILogger logger, int die1, int die2, int sum);
}
