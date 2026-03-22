namespace Askyl.Dsm.WebHosting.Tools.Extensions;

/// <summary>
/// Extension methods for building URIs with query parameters.
/// </summary>
public static partial class UriExtensions
{
    extension(bool value)
    {
        /// <summary>
        /// Converts a boolean to lowercase invariant string representation.
        /// </summary>
        public string ToLower()
            => value ? "true" : "false";
    }

    extension(string uri)
    {
        /// <summary>
        /// Builds a URI with query parameters from a base path.
        /// </summary>
        public string WithQuery(params (string key, string value)[] parameters)
        {
            if (parameters.Length == 0)
            {
                return uri;
            }

            var pairs = parameters.Where(p => p.value is not null).Select(p => $"{Uri.EscapeDataString(p.key)}={Uri.EscapeDataString(p.value)}");
            var query = String.Join("&", pairs);

            return String.IsNullOrEmpty(query) ? uri : $"{uri}?{query}";
        }
    }
}
