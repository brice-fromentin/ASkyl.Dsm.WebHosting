using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.AppPortal.ReverseProxy;

public class ReverseProxyDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<ReverseProxyUuids>(informations)
{
    public override string Name => ApiConstants.AppPortalReverseProxy;

    protected override string JsonParameterName => "uuids";

    public override int Version => 1;

    public override string Method => ApiConstants.MethodDelete;

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}
