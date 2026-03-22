using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationCheckPermissionParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCheckPermission>(informations)
{
    public override string Name => ApiNames.FileStationCheckPermission;

    public override int Version => 3;

    public override string Method => "write";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
