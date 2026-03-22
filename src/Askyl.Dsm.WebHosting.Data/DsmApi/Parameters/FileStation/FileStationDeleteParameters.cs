using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationDelete>(informations)
{
    public override string Name => ApiNames.FileStationDelete;

    public override int Version => 2;

    public override string Method => ApiMethods.Delete;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
