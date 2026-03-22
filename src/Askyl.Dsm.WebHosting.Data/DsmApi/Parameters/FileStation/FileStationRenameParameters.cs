using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationRenameParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationRename>(informations)
{
    public override string Name => ApiNames.FileStationRename;

    public override int Version => 2;

    public override string Method => "rename";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
