using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.JSON;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;

/// <summary>
/// Base class for DSM API request parameters.
/// T is expected to be a record with init setters (immutable after construction).
/// No cloning needed — init setters prevent mutation.
/// </summary>
public abstract class ApiParametersBase<T> : IApiParameters where T : class, new()
{
    private ApiParametersBase() => throw new NotImplementedException();

    protected ApiParametersBase(T? entry = null)
    {
        Parameters = entry ?? new();
    }

    #region Reflections Caches

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

    public abstract string Name { get; }

    public abstract int Version { get; }

    public abstract string Method { get; }

    public abstract SerializationFormats SerializationFormat { get; }

    public T Parameters { get; }

    protected virtual string JsonParameterName => throw new NotImplementedException();

    public string BuildUrl(string server, int port, string path)
        => $"https://{server}:{port}/webapi/{path}/{Name}";

    public StringContent ToForm()
    {
        var content = BuildForm().ToString();

        return new(content, Encoding.UTF8, "application/x-www-form-urlencoded");
    }

    public StringContent ToJson()
    {
        var builder = BuildForm(true);
        var serialized = JsonSerializer.Serialize(Parameters, JsonOptionsCache.Options);
        var encoded = Uri.EscapeDataString(serialized);

        builder.Append('&');
        builder.Append(JsonParameterName);
        builder.Append('=');
        builder.Append(encoded);

        var content = builder.ToString();

        return new(content, Encoding.UTF8, "application/x-www-form-urlencoded");
    }

    private StringBuilder BuildForm(bool skipParameters = false)
    {
        var builder = new StringBuilder();

        builder.Append("api=").Append(Name);
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
                var serialized = SerializeValue(value);

                if (!String.IsNullOrEmpty(serialized))
                {
                    builder.Append('&');
                    builder.Append(name);
                    builder.Append('=');
                    builder.Append(Uri.EscapeDataString(serialized));
                }
            }
        }

        return builder;
    }

    private static string? SerializeValue(object value)
    {
        if (value is IEnumerable and not string)
        {
            return JsonSerializer.Serialize(value, JsonOptionsCache.Options);
        }

        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        var result = value.ToString();

        return String.IsNullOrEmpty(result) ? null : result;
    }
}
