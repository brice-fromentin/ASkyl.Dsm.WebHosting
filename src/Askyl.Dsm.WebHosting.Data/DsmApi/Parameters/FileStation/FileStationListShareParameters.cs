using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationListShareParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationListShare>(informations)
{
    public override string Name => ApiNames.FileStationList;

    public override int Version => 2;

    public override string Method => "list_share";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
