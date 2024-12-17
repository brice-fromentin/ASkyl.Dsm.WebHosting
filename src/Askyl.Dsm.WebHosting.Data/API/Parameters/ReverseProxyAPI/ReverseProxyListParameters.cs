using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.ReverseProxyAPI;

public class ReverseProxyListParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmDefaults.DsmApiReverseProxy;

    public override int Version => 1;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Query;
}