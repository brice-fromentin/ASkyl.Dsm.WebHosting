using Askyl.Dsm.WebHosting.Constants.DSM.API;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.AppPortal.ReverseProxy;

public class ReverseProxyListParameters()
    : ApiParametersBase<ApiParametersNone>()
{
    public override string Name => ApiConstants.AppPortalReverseProxy;

    public override int Version => 1;

    public override string Method => ApiConstants.MethodList;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
