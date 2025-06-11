using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationBackgroundTaskListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationBackgroundTask>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationBackgroundTask;

    public override int Version => 3;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationBackgroundTaskClearFinishedParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationBackgroundTask;

    public override int Version => 3;

    public override string Method => "clear_finished";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
