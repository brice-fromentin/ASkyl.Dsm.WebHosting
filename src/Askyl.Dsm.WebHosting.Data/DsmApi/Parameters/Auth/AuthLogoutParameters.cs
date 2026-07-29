using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Auth;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Auth;

/// <summary>
/// Parameters for SYNO.API.Auth.logout — invalidates the SID on the NAS.
/// Version 6 matches <see cref="AuthLoginParameters"/>: it is the version this application already
/// negotiates successfully for this API, so it is the safest choice for its counterpart.
/// </summary>
public class AuthLogoutParameters()
    : ApiParametersBase<AuthenticateLogout>()
{
    public override string Name => ApiConstants.Auth;

    public override int Version => 6;

    public override string Method => ApiConstants.MethodLogout;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
