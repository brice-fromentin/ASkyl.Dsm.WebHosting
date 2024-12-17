using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Data.Attributes;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters;

public abstract class ApiParametersBase<T> : IApiParameters where T : class, new()
{
    private ApiParametersBase() => throw new NotImplementedException();

    public ApiParametersBase(ApiInformationCollection informations)
    {
        var infos = (this.Name == DsmDefaults.DsmApiInfo)
                        ? new() { Path = DsmDefaults.DsmApiHandshakePath, MinVersion = 1, MaxVersion = 7 }
                        : informations.Get(this.Name) ?? throw new NullReferenceException("Empty API Information."); ;

        if (this.Version < infos.MinVersion || this.Version > infos.MaxVersion)
        {
            throw new ArgumentOutOfRangeException($"Requested API version is {this.Version}, but {this.Name} support is between {infos.MinVersion} and {infos.MaxVersion}.");
        }

        this.Path = infos.Path;
    }

    #region Reflections Caches

    private static List<PropertyDefinition> Properties { get; } = typeof(T).GetProperties().Select(x => PropertyDefinition.Create(x)).ToList();

    private class PropertyDefinition(PropertyInfo info, string customName, bool requiresEncoding)
    {
        public PropertyInfo Info { get; } = info;

        public string CustomName { get; } = customName;

        public bool RequiresEncoding { get; } = requiresEncoding;

        public static PropertyDefinition Create (PropertyInfo info)
        {
            return new PropertyDefinition
            (
                info,
                info.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? "",
                info.CustomAttributes.Any(x => x.AttributeType == typeof(UrlEncodeAttribute))
            );
        }
    }

    #endregion

    public abstract string Name { get; }

    public string Path { get; }

    public abstract int Version { get; }

    public abstract string Method { get; }

    public abstract SerializationFormats SerializationFormat { get; }

    public T Parameters { get; } = new();

    public string BuildUrl(string server, int port)
    {
        var baseUrl = this.SerializationFormat switch
        {
            SerializationFormats.Query
                => $"https://{server}:{port}/webapi/{this.Path}?api={this.Name}&version={this.Version}&method={this.Method}",
            SerializationFormats.Form
                => $"https://{server}:{port}/webapi/{this.Path}?api={this.Name}",
            SerializationFormats.Json
                => $"https://{server}:{port}/webapi/{this.Path}/{this.Name}",
            _
                => throw new NotSupportedException($"SerializationFormat : {this.SerializationFormat} not supported.")
        };

        return baseUrl + this.ToQueryString();
    }

    private string ToQueryString()
        => (this.SerializationFormat != SerializationFormats.Query) ? "" : this.BuildQueryOrForm();

    public string ToForm()
        => BuildQueryOrForm();

    private string BuildQueryOrForm()
    {
        var builder = new StringBuilder();

        if (this.SerializationFormat == SerializationFormats.Form)
        {
            builder.Append("version=").Append(this.Version);
            builder.Append("&method=").Append(this.Method);
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

                serialized = (property.RequiresEncoding) ? Uri.EscapeDataString(serialized) : serialized;

                if (builder.Length > 0)
                {
                    builder.Append("&");
                }

                builder.Append(name);
                builder.Append("=");
                builder.Append(serialized);
            }
        }

        if (builder.Length > 0)
        {
            builder.Insert(0, "&");
        }

        return builder.ToString();
    }

    public string ToJson()
        => JsonSerializer.Serialize(this.Parameters);
}
