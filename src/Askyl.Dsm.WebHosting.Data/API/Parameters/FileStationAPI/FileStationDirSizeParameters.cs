using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationDirSizeStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationDirSize>(informations)
{
    public override string Name => DsmApiNames.FileStationDirSize;

    public override int Version => 2;

    public override string Method => "start";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationDirSizeStatusParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.FileStationDirSize;

    public override int Version => 2;

    public override string Method => "status";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationDirSizeStopParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.FileStationDirSize;

    public override int Version => 2;

    public override string Method => "stop";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
