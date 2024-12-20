using System.Text;
using BenchmarkDotNet.Attributes;

namespace Askyl.Dsm.WebHosting.Benchmarks;

[MemoryDiagnoser]
public class StringBuilderBenchmark
{
    private const string _server = "localhost";
    private const int _port = 80;
    private const string _path = "entry.cgi";
    private const string _api = "SYNO.API.Info";
    private const int _version = 1;
    private const string _method = "query";
    private readonly Dictionary<string, string> _parameters = Enumerable.Range(1, 10)
                                                                        .ToDictionary(i => $"parameter{i}", i => $"value{i}");

    [Benchmark]
    public void UrlInterpolatedString()
    {
        var url = $"https://{_server}:{_port}/webapi/{_path}?api={_api}";
    }

    [Benchmark]
    public void UrlBuilder()
    {
        var builder = new StringBuilder();

        builder.Append("https://");
        builder.Append(_server);
        builder.Append(':');
        builder.Append(_port);
        builder.Append("/webapi/");
        builder.Append(_path);
        builder.Append("?api=");
        builder.Append(_api);

        var url = builder.ToString();
    }

    [Benchmark]
    public void ParametersInterpolatedString()
    {
        var parameters = $"api={_api}&version={_version}&method={_method}";

        foreach(var pair in _parameters)
        {
            parameters = $"{parameters}&{pair.Key}={pair.Value}";
        }
    }

    [Benchmark]
    public void ParametersBuilder()
    {
        var builder = new StringBuilder();

        builder.Append("api=").Append(_api);
        builder.Append("&version=").Append(_version);
        builder.Append("&method=").Append(_method);

        foreach(var pair in _parameters)
        {
            builder.Append('&');
            builder.Append(pair.Key);
            builder.Append('=');
            builder.Append(pair.Value);
        }

        var parameters = builder.ToString();
    }
}
