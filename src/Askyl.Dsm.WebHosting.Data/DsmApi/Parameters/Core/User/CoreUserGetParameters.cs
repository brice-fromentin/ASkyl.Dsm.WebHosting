using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.User;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.User;

/// <summary>
/// Parameters for fetching a single user by name via SYNO.Core.User.get.
/// Used to validate whether the logged-in user still exists and the session is active.
/// </summary>
public class CoreUserGetParameters(CoreUserGetEntry? entry = null)
    : ApiParametersBase<CoreUserGetEntry>(entry)
{
    public override string Name => ApiConstants.CoreUser;

    public override int Version => 1;

    public override string Method => ApiConstants.MethodGet;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
