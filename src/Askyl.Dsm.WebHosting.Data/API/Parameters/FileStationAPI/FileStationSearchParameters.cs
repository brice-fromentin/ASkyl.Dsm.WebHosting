using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationSearchStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => DsmApiNames.FileStationSearch;

    public override int Version => 2;

    public override string Method => DsmApiMethods.Start;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSearchListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => DsmApiNames.FileStationSearch;

    public override int Version => 2;

    public override string Method => DsmApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSearchStopParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => DsmApiNames.FileStationSearch;

    public override int Version => 2;

    public override string Method => DsmApiMethods.Stop;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
