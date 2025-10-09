using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationSharingListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSharing>(informations)
{
    public override string Name => DsmApiNames.FileStationSharing;

    public override int Version => 3;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSharingCreateParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSharing>(informations)
{
    public override string Name => DsmApiNames.FileStationSharing;

    public override int Version => 3;

    public override string Method => "create";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSharingDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSharing>(informations)
{
    public override string Name => DsmApiNames.FileStationSharing;

    public override int Version => 3;

    public override string Method => "delete";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSharingEditParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSharing>(informations)
{
    public override string Name => DsmApiNames.FileStationSharing;

    public override int Version => 3;

    public override string Method => "edit";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSharingClearInvalidParameters(ApiInformationCollection informations) : ApiParametersBase<ApiParametersNone>(informations)
{
    public override string Name => DsmApiNames.FileStationSharing;

    public override int Version => 3;

    public override string Method => "clear_invalid";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
