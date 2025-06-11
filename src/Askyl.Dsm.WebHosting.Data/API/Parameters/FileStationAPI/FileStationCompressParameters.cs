using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationCompressStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCompress>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationCompress;

    public override int Version => 3;

    public override string Method => "start";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCompressStatusParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationCompress;

    public override int Version => 3;

    public override string Method => "status";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCompressStopParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationCompress;

    public override int Version => 3;

    public override string Method => "stop";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
