using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.ReverseProxyAPI;

public class ReverseProxyListParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.ReverseProxy;

    public override int Version => 1;

    public override string Method => DsmApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}