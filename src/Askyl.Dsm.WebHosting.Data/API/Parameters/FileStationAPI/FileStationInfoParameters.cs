using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationInfoParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationInfo;

    public override int Version => 2;

    public override string Method => "get";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
