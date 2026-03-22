using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.AuthenticationAPI;

public class AuthenticationLoginParameters(ApiInformationCollection informations) : ApiParametersBase<AuthenticateLogin>(informations)
{
    public override string Name => ApiNames.Auth;

    public override int Version => 6;

    public override string Method => "login";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}