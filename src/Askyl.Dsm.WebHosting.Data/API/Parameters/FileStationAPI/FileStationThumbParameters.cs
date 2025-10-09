using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationThumbGetParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationThumb>(informations)
{
    public override string Name => DsmApiNames.FileStationThumb;

    public override int Version => 2;

    public override string Method => DsmApiMethods.Get;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
