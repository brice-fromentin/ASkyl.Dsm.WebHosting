using System;

namespace Askyl.Dsm.WebHosting.Data.API;

public class ApiParameters
{
    public string Name { get; set; } = default!;

    public string Path { get; set; } = default!;

    public int Version { get; set; }

    public string Method { get; set; } = default!;

    public Dictionary<string, string> Parameters { get; set; } = [];

    public static ApiParameters Create(string name, string path, int version, string method, params KeyValuePair<string, string>[] parameters)
    {
        return new ApiParameters
        {
            Name = name,
            Path = path,
            Version = version,
            Method = method,
            Parameters = parameters.ToDictionary()
        };
    }
}
