using System;
using Askyl.Dsm.WebHosting.Constants;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters;

public interface IApiParameters
{
    public string Name { get; }

    public string Path { get; }

    public int Version { get; }

    public string Method { get; }

    public SerializationFormats SerializationFormat { get; }

    public string BuildUrl(string server, int port);

    public StringContent ToForm();

    public StringContent ToJson();
}
