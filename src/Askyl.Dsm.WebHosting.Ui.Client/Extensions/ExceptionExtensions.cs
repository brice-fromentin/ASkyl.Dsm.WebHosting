namespace Askyl.Dsm.WebHosting.Ui.Client.Extensions;

/// <summary>
/// Tells the failures that mean the server never answered from the ones it reported.
/// </summary>
public static class ExceptionExtensions
{
    extension(Exception exception)
    {
        /// <summary>
        /// What the browser's HttpClient throws once its wait runs out (TaskCanceledException), or when the server
        /// cannot be reached at all. Uncaught, the first one reads to Blazor as a cancelled task and the second one
        /// ends on its "An unhandled error has occurred" bar.
        /// </summary>
        public bool IsNoAnswer => exception is TaskCanceledException or HttpRequestException;
    }
}
