using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationCheckPermissionParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCheckPermission>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationCheckPermission;

    public override int Version => 3;

    public override string Method => "write";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
