using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationSearchStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => ApiNames.FileStationSearch;

    public override int Version => 2;

    public override string Method => ApiMethods.Start;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSearchListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => ApiNames.FileStationSearch;

    public override int Version => 2;

    public override string Method => ApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSearchStopParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => ApiNames.FileStationSearch;

    public override int Version => 2;

    public override string Method => ApiMethods.Stop;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
