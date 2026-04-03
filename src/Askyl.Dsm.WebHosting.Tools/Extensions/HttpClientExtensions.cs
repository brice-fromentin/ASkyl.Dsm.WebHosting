using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.JSON;
using Askyl.Dsm.WebHosting.Constants.Network;

namespace Askyl.Dsm.WebHosting.Tools.Extensions;

/// <summary>
/// Extension methods for HttpClient, HttpContent, and API responses that provide convenient JSON serialization methods.
/// </summary>
public static class HttpClientExtensions
{
    extension(HttpClient client)
    {
        /// <summary>
        /// Sends a GET request and deserializes the response to the specified type.
        /// Returns null if the response is unsuccessful or deserialization fails.
        /// </summary>
        public async Task<TResponse?> GetJsonAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default) where TResponse : class
        {
            var response = await client.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync(content, typeof(TResponse), JsonOptionsCache.Options, cancellationToken) as TResponse;
        }

        /// <summary>
        /// Sends a GET request and deserializes the response to the specified type.
        /// Returns the result of the factory function if the response is null or unsuccessful.
        /// </summary>
        public async Task<TResponse> GetJsonOrDefaultAsync<TResponse>(string requestUri, Func<TResponse> defaultValueFactory, CancellationToken cancellationToken = default) where TResponse : class
            => await client.GetJsonAsync<TResponse>(requestUri, cancellationToken) ?? defaultValueFactory();

        /// <summary>
        /// Sends a POST request with JSON content and deserializes the response to the specified type.
        /// Returns null if the response is unsuccessful or deserialization fails.
        /// </summary>
        public async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(string requestUri, TRequest? content, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
        {
            var jsonContent = content is not null
                ? new StringContent(JsonSerializer.Serialize(content, JsonOptionsCache.Options), System.Text.Encoding.UTF8, NetworkConstants.ApplicationJson)
                : null;

            using (jsonContent)
            {
                var response = await client.PostAsync(requestUri, jsonContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return default;
                }

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync(stream, typeof(TResponse), JsonOptionsCache.Options, cancellationToken) as TResponse;
            }
        }

        /// <summary>
        /// Sends a POST request with JSON content and deserializes the response to the specified type.
        /// Returns the result of the factory function if the response is null or unsuccessful.
        /// </summary>
        public async Task<TResponse> PostJsonOrDefaultAsync<TRequest, TResponse>(string requestUri, TRequest? content, Func<TResponse> defaultValueFactory, CancellationToken cancellationToken = default) where TRequest : class where TResponse : class
            => await client.PostJsonAsync<TRequest, TResponse>(requestUri, content, cancellationToken) ?? defaultValueFactory();

        /// <summary>
        /// Sends a DELETE request and deserializes the response to the specified type.
        /// Returns null if the response is unsuccessful or deserialization fails.
        /// </summary>
        public async Task<TResponse?> DeleteJsonAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default) where TResponse : class
        {
            var response = await client.DeleteAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync(content, typeof(TResponse), JsonOptionsCache.Options, cancellationToken) as TResponse;
        }

        /// <summary>
        /// Sends a DELETE request and deserializes the response to the specified type.
        /// Returns the result of the factory function if the response is null or unsuccessful.
        /// </summary>
        public async Task<TResponse> DeleteJsonOrDefaultAsync<TResponse>(string requestUri, Func<TResponse> defaultValueFactory, CancellationToken cancellationToken = default) where TResponse : class
            => await client.DeleteJsonAsync<TResponse>(requestUri, cancellationToken) ?? defaultValueFactory();
    }

    extension(HttpContent content)
    {
        /// <summary>
        /// Deserializes the HTTP content to an object of the specified type using cached JSON options.
        /// </summary>
        public async Task<T?> ReadFromJsonAsync<T>(CancellationToken cancellationToken = default) where T : class
            => await JsonSerializer.DeserializeAsync(await content.ReadAsStreamAsync(cancellationToken), typeof(T), JsonOptionsCache.Options, cancellationToken) as T;
    }
}
