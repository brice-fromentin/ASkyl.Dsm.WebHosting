using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationDownloadParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationDownload>(informations)
{
    public override string Name => ApiNames.FileStationDownload;

    public override int Version => 2;

    public override string Method => "download";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
