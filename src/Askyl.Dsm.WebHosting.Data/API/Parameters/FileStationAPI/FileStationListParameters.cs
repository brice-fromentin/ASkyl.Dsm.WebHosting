using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationList>(informations)
{
    public override string Name => DsmApiNames.FileStationList;

    public override int Version => 2;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
