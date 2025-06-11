using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationDelete>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationDelete;

    public override int Version => 2;

    public override string Method => "delete";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
