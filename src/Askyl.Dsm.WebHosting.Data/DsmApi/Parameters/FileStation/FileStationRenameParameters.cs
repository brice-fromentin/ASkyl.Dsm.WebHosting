using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationRenameParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationRename>(informations)
{
    public override string Name => ApiNames.FileStationRename;

    public override int Version => 2;

    public override string Method => "rename";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
