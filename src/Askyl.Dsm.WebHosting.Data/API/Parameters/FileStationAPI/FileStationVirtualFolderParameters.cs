using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationVirtualFolderListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationVirtualFolder>(informations)
{
    public override string Name => ApiNames.FileStationVirtualFolder;

    public override int Version => 2;

    public override string Method => ApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
