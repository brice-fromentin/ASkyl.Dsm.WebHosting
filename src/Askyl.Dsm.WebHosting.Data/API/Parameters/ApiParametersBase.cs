using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Data.Attributes;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters;

public abstract class ApiParametersBase<T> : IApiParameters where T : class, IGenericCloneable<T>, new()
{
    private ApiParametersBase() => throw new NotImplementedException();

    public ApiParametersBase(ApiInformationCollection informations, T? entry = null)
    {
        var infos = (Name == DsmApiNames.Info)
                        ? CreateDefaultHandshakeInfo()
                        : informations.Get(Name) ?? throw new NullReferenceException("Empty API Information.");

        if (Version < infos.MinVersion || Version > infos.MaxVersion)
        {
            throw new ArgumentOutOfRangeException($"Requested API version is {Version}, but {Name} support is between {infos.MinVersion} and {infos.MaxVersion}.");
        }

        Path = infos.Path;
        Parameters = (entry is null) ? new() : entry.Clone();
    }

    /// <summary>
    /// Creates default API information for handshake operations.
    /// </summary>
    /// <returns>Default ApiInformation for handshake API.</returns>
    private static ApiInformation CreateDefaultHandshakeInfo()
        => new() { Path = DsmApiNames.Handshake, MinVersion = DsmApiVersions.MinVersion, MaxVersion = DsmApiVersions.MaxVersion };

    #region Reflections Caches

    private static string? ApiJsonParameterName;
    private static List<PropertyDefinition> Properties { get; } = GetDefinitions();

    private class PropertyDefinition(PropertyInfo info, string customName)
    {
        public PropertyInfo Info { get; } = info;

        public string CustomName { get; } = customName;

        public static PropertyDefinition Create(PropertyInfo info)
            => new(info, info.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? String.Empty);
    }

    private static List<PropertyDefinition> GetDefinitions()
        => [.. typeof(T).GetProperties().Select(PropertyDefinition.Create)];

    #endregion

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IndentCharacter = ' ',
        IndentSize = 4,
        WriteIndented = true
    };

    public abstract string Name { get; }

    public string Path { get; }

    public abstract int Version { get; }

    public abstract string Method { get; }

    public abstract SerializationFormats SerializationFormat { get; }

    public T Parameters { get; }

    public string BuildUrl(string server, int port) => $"https://{server}:{port}/webapi/{Path}/{Name}";

    public StringContent ToForm()
    {
        var content = BuildForm().ToString();

        return new(content, Encoding.UTF8, "application/x-www-form-urlencoded");
    }

    public StringContent ToJson()
    {
        ApiJsonParameterName ??= GetType().GetCustomAttribute<DsmParameterNameAttribute>()?.Name;

        if (String.IsNullOrWhiteSpace(ApiJsonParameterName))
        {
            throw new ArgumentException($"DsmParameterNameAttribute is not set for type {typeof(T).Name}");
        }

        var builder = BuildForm(true);

        builder.Append('&');
        builder.Append(ApiJsonParameterName);
        builder.Append('=');
        builder.Append(JsonSerializer.Serialize(Parameters, JsonOptions));

        var content = builder.ToString();

        return new(content, Encoding.UTF8, "application/x-www-form-urlencoded");
    }

    private StringBuilder BuildForm(bool skipParameters = false)
    {
        var builder = new StringBuilder();

        builder.Append("&api=").Append(Name);
        builder.Append("&version=").Append(Version);
        builder.Append("&method=").Append(Method);

        if (skipParameters)
        {
            return builder;
        }

        foreach (var property in Properties)
        {
            var value = property.Info.GetValue(Parameters);

            if (value is not null)
            {
                var name = (property.CustomName.Length == 0) ? property.Info.Name.ToLowerInvariant() : property.CustomName;
                var serialized = value?.ToString();

                if (serialized is null)
                {
                    continue;
                }

                builder.Append('&');
                builder.Append(name);
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(serialized));
            }
        }

        return builder;
    }
}
