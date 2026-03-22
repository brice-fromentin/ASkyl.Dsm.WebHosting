using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationCreateFolderParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCreateFolder>(informations)
{
    public override string Name => ApiNames.FileStationCreateFolder;

    public override int Version => 2;

    public override string Method => "create";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
