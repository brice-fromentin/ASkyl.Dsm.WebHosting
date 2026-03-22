using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationExtractStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationExtract>(informations)
{
    public override string Name => ApiNames.FileStationExtract;

    public override int Version => 2;

    public override string Method => "start";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationExtractStatusParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.FileStationExtract;

    public override int Version => 2;

    public override string Method => "status";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationExtractStopParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.FileStationExtract;

    public override int Version => 2;

    public override string Method => "stop";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationExtractListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationExtract>(informations)
{
    public override string Name => ApiNames.FileStationExtract;

    public override int Version => 2;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
