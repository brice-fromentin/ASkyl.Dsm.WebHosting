using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.UserSettings;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.UserSettings;

/// <summary>
/// Parameters for SYNO.Core.UserSettings.get — fetches all user settings.
/// Uses method "get" (v1), no payload required.
/// </summary>
public class CoreUserSettingsParameters(ApiInformationCollection informations)
    : ApiParametersBase<CoreUserSettingsEntry>(informations)
{
    public override string Name => ApiNames.CoreUserSettings;

    public override int Version => 1;

    public override string Method => ApiMethods.Get;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
