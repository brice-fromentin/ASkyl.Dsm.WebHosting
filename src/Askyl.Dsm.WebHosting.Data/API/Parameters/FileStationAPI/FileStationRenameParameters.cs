using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationRenameParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationRename>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationRename;

    public override int Version => 2;

    public override string Method => "rename";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
