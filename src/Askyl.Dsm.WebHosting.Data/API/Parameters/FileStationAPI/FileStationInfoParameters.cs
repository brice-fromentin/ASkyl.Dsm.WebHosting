using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationInfoParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.FileStationInfo;

    public override int Version => 2;

    public override string Method => "get";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
