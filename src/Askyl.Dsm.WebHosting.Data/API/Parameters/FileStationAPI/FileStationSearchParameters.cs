using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationSearchStartParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationSearch;

    public override int Version => 2;

    public override string Method => "start";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSearchListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationSearch;

    public override int Version => 2;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationSearchStopParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationSearch>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationSearch;

    public override int Version => 2;

    public override string Method => "stop";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
