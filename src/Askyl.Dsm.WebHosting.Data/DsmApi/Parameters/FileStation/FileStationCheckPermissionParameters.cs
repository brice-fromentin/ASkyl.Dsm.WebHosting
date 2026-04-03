using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationCheckPermissionParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCheckPermission>(informations)
{
    public override string Name => ApiNames.FileStationCheckPermission;

    public override int Version => 3;

    public override string Method => "write";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
