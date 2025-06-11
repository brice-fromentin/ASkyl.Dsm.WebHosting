using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationDownloadParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationDownload>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationDownload;

    public override int Version => 2;

    public override string Method => "download";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
