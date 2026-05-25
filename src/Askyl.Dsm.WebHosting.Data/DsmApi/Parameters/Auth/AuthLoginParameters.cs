using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Auth;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Auth;

public class AuthLoginParameters(ApiInformationCollection informations, AuthenticateLogin? entry = null)
    : ApiParametersBase<AuthenticateLogin>(informations, entry)
{
    public override string Name => ApiNames.Auth;

    public override int Version => 6;

    public override string Method => "login";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
