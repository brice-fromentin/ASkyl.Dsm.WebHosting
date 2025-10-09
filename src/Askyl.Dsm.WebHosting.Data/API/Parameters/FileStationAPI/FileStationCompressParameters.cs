using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationCompressStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCompress>(informations)
{
    public override string Name => DsmApiNames.FileStationCompress;

    public override int Version => 3;

    public override string Method => DsmApiMethods.Start;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCompressStatusParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.FileStationCompress;

    public override int Version => 3;

    public override string Method => DsmApiMethods.Status;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCompressStopParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.FileStationCompress;

    public override int Version => 3;

    public override string Method => DsmApiMethods.Stop;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
