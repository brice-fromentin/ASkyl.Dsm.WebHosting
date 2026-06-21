using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.AppPortal.ReverseProxy;

public class ReverseProxyCreateParameters(ApiInformationCollection informations, Models.ReverseProxy.ReverseProxy? entry = null)
    : ApiParametersBase<Models.ReverseProxy.ReverseProxy>(informations, entry)
{
    public override string Name => ApiConstants.AppPortalReverseProxy;

    protected override string JsonParameterName => "entry";

    public override int Version => 1;

    public override string Method => ApiConstants.MethodCreate;

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}
