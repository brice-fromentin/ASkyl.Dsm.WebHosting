using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationBackgroundTaskListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationBackgroundTask>(informations)
{
    public override string Name => ApiNames.FileStationBackgroundTask;

    public override int Version => 3;

    public override string Method => ApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationBackgroundTaskClearFinishedParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => ApiNames.FileStationBackgroundTask;

    public override int Version => 3;

    public override string Method => "clear_finished";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
