using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationMd5GetParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationMd5>(informations)
{
    public override string Name => DsmApiNames.FileStationMd5;

    public override int Version => 2;

    public override string Method => DsmApiMethods.Get;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
