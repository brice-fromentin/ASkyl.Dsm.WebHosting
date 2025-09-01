using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Data.Attributes;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.ReverseProxyAPI;

[DsmParameterName("uuids")]
public class ReverseProxyDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<ReverseProxyUuids>(informations)
{
    public override string Name => DsmApiNames.ReverseProxy;

    public override int Version => 1;

    public override string Method => DsmApiMethods.Delete;

    public override SerializationFormats SerializationFormat => SerializationFormats.Json;
}

/*
api=SYNO.Core.AppPortal.ReverseProxy
&method=delete
&version=1
&uuids=["20409e24-43fa-4239-9199-e42f330356bb"]
*/