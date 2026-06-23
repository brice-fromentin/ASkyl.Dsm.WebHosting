using Askyl.Dsm.WebHosting.Constants.DSM.API;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.AppPortal.ReverseProxy;

public class ReverseProxyUpdateParameters(Models.ReverseProxy.ReverseProxy proxy)
    : ApiParametersBase<Models.ReverseProxy.ReverseProxy>(proxy)
{
    public override string Name => ApiConstants.AppPortalReverseProxy;

    protected override string JsonParameterName => "entry";

    public override int Version => 1;

    public override string Method => ApiConstants.MethodUpdate;

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}
