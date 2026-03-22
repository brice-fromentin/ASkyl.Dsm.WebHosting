using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

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
