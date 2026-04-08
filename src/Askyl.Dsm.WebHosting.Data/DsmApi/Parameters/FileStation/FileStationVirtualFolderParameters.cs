using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationVirtualFolderListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationVirtualFolder>(informations)
{
    public override string Name => ApiNames.FileStationVirtualFolder;

    public override int Version => 2;

    public override string Method => ApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
