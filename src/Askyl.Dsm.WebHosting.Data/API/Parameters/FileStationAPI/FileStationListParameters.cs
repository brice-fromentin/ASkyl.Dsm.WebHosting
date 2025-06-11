using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationList>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationList;

    public override int Version => 2;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
