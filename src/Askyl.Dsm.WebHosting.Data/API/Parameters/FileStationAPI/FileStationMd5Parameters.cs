using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationMd5GetParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationMd5>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationMd5;

    public override int Version => 2;

    public override string Method => "get";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
