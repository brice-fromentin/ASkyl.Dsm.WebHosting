using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.ReverseProxy;

public class ReverseProxyListParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.AppPortalReverseProxy;

    public override int Version => 1;

    public override string Method => ApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}