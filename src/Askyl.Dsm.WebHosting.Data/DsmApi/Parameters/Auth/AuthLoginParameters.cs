using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Auth;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Auth;

public class AuthLoginParameters(AuthenticateLogin? entry = null)
    : ApiParametersBase<AuthenticateLogin>(entry)
{
    public override string Name => ApiConstants.Auth;

    public override int Version => 6;

    public override string Method => ApiConstants.MethodLogin;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
