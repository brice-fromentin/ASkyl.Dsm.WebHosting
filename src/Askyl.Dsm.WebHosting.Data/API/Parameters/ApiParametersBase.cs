using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Data.Attributes;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters;

public abstract class ApiParametersBase<T> : IApiParameters where T : class, IGenericCloneable<T>, new()
{
    private ApiParametersBase() => throw new NotImplementedException();

    public ApiParametersBase(ApiInformationCollection informations, T? entry = null)
    {
        var infos = (this.Name == DsmDefaults.DsmApiInfo)
                        ? new() { Path = DsmDefaults.DsmApiHandshakePath, MinVersion = 1, MaxVersion = 7 }
                        : informations.Get(this.Name) ?? throw new NullReferenceException("Empty API Information.");

        if (this.Version < infos.MinVersion || this.Version > infos.MaxVersion)
        {
            throw new ArgumentOutOfRangeException($"Requested API version is {this.Version}, but {this.Name} support is between {infos.MinVersion} and {infos.MaxVersion}.");
        }

        this.Path = infos.Path;
        this.Parameters = (entry == null) ? new() : entry.Clone();
    }

    #region Reflections Caches

    private static string? ApiJsonParameterName;
    private static List<PropertyDefinition> Properties { get; } = GetDefinitions();

    private class PropertyDefinition(PropertyInfo info, string customName)
    {
        public PropertyInfo Info { get; } = info;

        public string CustomName { get; } = customName;

        public static PropertyDefinition Create(PropertyInfo info)
            => new(info, info.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? "");
    }

    private static List<PropertyDefinition> GetDefinitions()
        => [.. typeof(T).GetProperties().Select(x => PropertyDefinition.Create(x))];

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

    public string BuildUrl(string server, int port) => $"https://{server}:{port}/webapi/{this.Path}/{this.Name}";

    public StringContent ToForm()
    {
        var content = BuildForm().ToString();

        return new (content, Encoding.UTF8, "application/x-www-form-urlencoded");
    }

    public StringContent ToJson()
    {
        ApiJsonParameterName??= this.GetType().GetCustomAttribute<DsmParameterNameAttribute>()?.Name;

        if (String.IsNullOrWhiteSpace(ApiJsonParameterName))
        {
            throw new ArgumentException($"DsmParameterNameAttribute is not set for type {typeof(T).Name}");
        }

        var builder = this.BuildForm(true);

        builder.Append('&');
        builder.Append(ApiJsonParameterName);
        builder.Append('=');
        builder.Append(JsonSerializer.Serialize(this.Parameters, JsonOptions));

        var content = builder.ToString();

        return new(content, Encoding.UTF8, "application/x-www-form-urlencoded");
    }

    private StringBuilder BuildForm(bool skipParameters = false)
    {
        var builder = new StringBuilder();

        builder.Append("&api=").Append(this.Name);
        builder.Append("&version=").Append(this.Version);
        builder.Append("&method=").Append(this.Method);

        if (skipParameters)
        {
            return builder;
        }

        foreach (var property in Properties)
        {
            var value = property.Info.GetValue(this.Parameters);

            if (value is not null)
            {
                var name = (property.CustomName.Length == 0) ? property.Info.Name.ToLowerInvariant() : property.CustomName;
                var serialized = value.ToString();

                if (serialized is null)
                {
                    continue;
                }

                builder.Append('&');
                builder.Append(name);
                builder.Append('=');
                builder.Append(serialized);
            }
        }

        return builder;
    }
}
