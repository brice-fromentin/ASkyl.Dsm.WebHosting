using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.Attributes;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.ReverseProxy;

[DsmParameterName("uuids")]
public class ReverseProxyDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<ReverseProxyUuids>(informations)
{
    public override string Name => ApiNames.AppPortalReverseProxy;

    public override int Version => 1;

    public override string Method => ApiMethods.Delete;

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}

/*
api=SYNO.Core.AppPortal.ReverseProxy
&method=delete
&version=1
&uuids=["20409e24-43fa-4239-9199-e42f330356bb"]
*/
