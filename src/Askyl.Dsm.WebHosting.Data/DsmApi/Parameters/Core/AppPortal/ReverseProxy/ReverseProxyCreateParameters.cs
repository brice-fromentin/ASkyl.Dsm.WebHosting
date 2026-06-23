using Askyl.Dsm.WebHosting.Constants.DSM.API;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.AppPortal.ReverseProxy;

public class ReverseProxyCreateParameters(Models.ReverseProxy.ReverseProxy? entry = null)
    : ApiParametersBase<Models.ReverseProxy.ReverseProxy>(entry)
{
    public override string Name => ApiConstants.AppPortalReverseProxy;

    protected override string JsonParameterName => "entry";

    public override int Version => 1;

    public override string Method => ApiConstants.MethodCreate;

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}
