using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationInfoParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.FileStationInfo;

    public override int Version => 2;

    public override string Method => "get";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
