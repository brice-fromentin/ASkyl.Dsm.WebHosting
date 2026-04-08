using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationDelete>(informations)
{
    public override string Name => ApiNames.FileStationDelete;

    public override int Version => 2;

    public override string Method => ApiMethods.Delete;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
