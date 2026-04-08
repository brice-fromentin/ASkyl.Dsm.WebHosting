using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationCopyParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCopyMove>(informations)
{
    public override string Name => ApiNames.FileStationCopyMove;

    public override int Version => 3;

    public override string Method => ApiMethods.Start;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationMoveParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationCopyMove>(informations)
{
    public override string Name => ApiNames.FileStationCopyMove;

    public override int Version => 3;

    public override string Method => ApiMethods.Start;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCopyMoveStatusParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.FileStationCopyMove;

    public override int Version => 3;

    public override string Method => ApiMethods.Status;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationCopyMoveStopParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.FileStationCopyMove;

    public override int Version => 3;

    public override string Method => ApiMethods.Stop;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
