using System;
using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.AuthenticationAPI;

public class AuthenticationLoginParameters(ApiInformationCollection informations) : ApiParametersBase<AuthenticateLogin>(informations)
{
    public override string Name => DsmDefaults.DsmApiAuth;

    public override int Version => 6;

    public override string Method => "login";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}