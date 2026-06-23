using Askyl.Dsm.WebHosting.Constants.DSM.API;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;

public interface IApiParameters
{
    public string Name { get; }

    public int Version { get; }

    public string Method { get; }

    public SerializationFormats SerializationFormat { get; }

    public string BuildUrl(string server, int port, string path);

    public StringContent ToForm();

    public StringContent ToJson();
}
