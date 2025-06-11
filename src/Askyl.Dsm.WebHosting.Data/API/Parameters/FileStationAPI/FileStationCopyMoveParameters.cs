using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationCopyParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCopyMove>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationCopyMove;

    public override int Version => 3;

    public override string Method => "start";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationMoveParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCopyMove>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationCopyMove;

    public override int Version => 3;

    public override string Method => "start";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCopyMoveStatusParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationCopyMove;

    public override int Version => 3;

    public override string Method => "status";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCopyMoveStopParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationCopyMove;

    public override int Version => 3;

    public override string Method => "stop";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
