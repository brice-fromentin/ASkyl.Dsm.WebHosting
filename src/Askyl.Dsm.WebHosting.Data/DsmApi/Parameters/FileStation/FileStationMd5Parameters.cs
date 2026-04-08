using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationMd5GetParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationMd5>(informations)
{
    public override string Name => ApiNames.FileStationMd5;

    public override int Version => 2;

    public override string Method => ApiMethods.Get;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
