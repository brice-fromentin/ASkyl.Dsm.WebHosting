using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationExtractStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationExtract>(informations)
{
    public override string Name => DsmApiNames.FileStationExtract;

    public override int Version => 2;

    public override string Method => "start";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationExtractStatusParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.FileStationExtract;

    public override int Version => 2;

    public override string Method => "status";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationExtractStopParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.FileStationExtract;

    public override int Version => 2;

    public override string Method => "stop";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationExtractListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationExtract>(informations)
{
    public override string Name => DsmApiNames.FileStationExtract;

    public override int Version => 2;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
