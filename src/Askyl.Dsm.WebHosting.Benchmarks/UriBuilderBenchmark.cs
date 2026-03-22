using System.Text;

using Askyl.Dsm.WebHosting.Tools.Extensions;

using BenchmarkDotNet.Attributes;

namespace Askyl.Dsm.WebHosting.Benchmarks;

[MemoryDiagnoser]
public class UriBuilderBenchmark
{
    private const string _basePath = "api/v1/filemanagement/directorycontents";
    private readonly string _path = "/volume1/web/myapp";
    private readonly bool _directoryOnly = true;
    private readonly (string key, string value)[] _parametersTupleArray = [("path", "/volume1/web/myapp"), ("directoryOnly", "true")];
    private readonly List<(string key, string value)> _parametersList = [("path", "/volume1/web/myapp"), ("directoryOnly", "true")];

    [Benchmark]
    public void CurrentStringConcatenation()
    {
        var url = _basePath +
                  "?path=" + Uri.EscapeDataString(_path) +
                  "&directoryOnly=" + _directoryOnly.ToLower();
    }

    [Benchmark]
    public void InterpolatedStringWithEscaping()
    {
        var url = $"{_basePath}?path={Uri.EscapeDataString(_path)}&directoryOnly={_directoryOnly.ToLower()}";
    }

    [Benchmark]
    public void StringBuilderQuery()
    {
        var builder = new StringBuilder();

        builder.Append(_basePath);
        builder.Append("?path=");
        builder.Append(Uri.EscapeDataString(_path));
        builder.Append("&directoryOnly=");
        builder.Append(_directoryOnly.ToLower());

        var url = builder.ToString();
    }

    [Benchmark]
    public void UriBuilderManualQuery()
    {
        var queryParts = new List<string>
        {
            $"path={Uri.EscapeDataString(_path)}",
            $"directoryOnly={_directoryOnly.ToLower()}"
        };

        var url = $"{_basePath}?{String.Join("&", queryParts)}";
    }

    [Benchmark]
    public void ExtensionMethodWithTuple()
    {
        var parameters = new[] { ("path", _path), ("directoryOnly", _directoryOnly.ToLower()) };
        var url = _basePath.WithQuery(parameters);
    }

    #region For Loop Variants

    [Benchmark]
    public void WithQueryForLoopList()
    {
        var queryParts = new List<string>();

        for (int i = 0; i < _parametersList.Count; i++)
        {
            var (key, value) = _parametersList[i];
            queryParts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        var url = $"{_basePath}?{String.Join("&", queryParts)}";
    }

    [Benchmark]
    public void WithQueryForLoopArray()
    {
        var queryParts = new List<string>();

        for (int i = 0; i < _parametersTupleArray.Length; i++)
        {
            var param = _parametersTupleArray[i];
            queryParts.Add($"{param.key}={Uri.EscapeDataString(param.value)}");
        }

        var url = $"{_basePath}?{String.Join("&", queryParts)}";
    }

    #endregion

    #region Foreach Loop Variants

    [Benchmark]
    public void WithQueryForeachList()
    {
        var queryParts = new List<string>();

        foreach (var (key, value) in _parametersList)
        {
            queryParts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        var url = $"{_basePath}?{String.Join("&", queryParts)}";
    }

    [Benchmark]
    public void WithQueryForeachArray()
    {
        var queryParts = new List<string>();

        foreach (var (key, value) in _parametersTupleArray)
        {
            queryParts.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        var url = $"{_basePath}?{String.Join("&", queryParts)}";
    }

    #endregion

    #region StringBuilder Variants

    [Benchmark]
    public void WithQueryStringBuilderList()
    {
        var builder = new StringBuilder();

        builder.Append(_basePath);
        builder.Append('?');

        for (int i = 0; i < _parametersList.Count; i++)
        {
            var (key, value) = _parametersList[i];

            if (i > 0)
                builder.Append('&');

            builder.Append(key);
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
        }

        _ = builder.ToString();
    }

    [Benchmark]
    public void WithQueryStringBuilderArray()
    {
        var builder = new StringBuilder();

        builder.Append(_basePath);
        builder.Append('?');

        for (int i = 0; i < _parametersTupleArray.Length; i++)
        {
            var (key, value) = _parametersTupleArray[i];

            if (i > 0)
                builder.Append('&');

            builder.Append(key);
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
        }

        _ = builder.ToString();
    }

    #endregion

    [Benchmark]
    public void UriBuilderManualMultipleParams()
    {
        var queryParts = new List<string>();
        queryParts.Add($"path={Uri.EscapeDataString(_path)}");
        queryParts.Add($"directoryOnly={_directoryOnly.ToLower()}");
        queryParts.Add("recursive=true");
        queryParts.Add("includeHidden=false");

        var url = $"{_basePath}?{String.Join("&", queryParts)}";
    }
}
