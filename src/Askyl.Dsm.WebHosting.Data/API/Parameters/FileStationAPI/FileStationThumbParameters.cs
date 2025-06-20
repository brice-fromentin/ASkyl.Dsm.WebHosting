using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationThumbGetParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationThumb>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationThumb;

    public override int Version => 2;

    public override string Method => "get";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
