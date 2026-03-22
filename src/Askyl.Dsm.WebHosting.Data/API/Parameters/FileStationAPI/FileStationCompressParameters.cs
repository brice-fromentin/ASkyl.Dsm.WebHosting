using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationCompressStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCompress>(informations)
{
    public override string Name => ApiNames.FileStationCompress;

    public override int Version => 3;

    public override string Method => ApiMethods.Start;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCompressStatusParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.FileStationCompress;

    public override int Version => 3;

    public override string Method => ApiMethods.Status;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCompressStopParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.FileStationCompress;

    public override int Version => 3;

    public override string Method => ApiMethods.Stop;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
