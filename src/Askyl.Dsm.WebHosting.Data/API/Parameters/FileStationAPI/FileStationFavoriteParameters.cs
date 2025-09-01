using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationFavoriteListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => DsmApiNames.FileStationFavorite;

    public override int Version => 2;

    public override string Method => DsmApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteAddParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => DsmApiNames.FileStationFavorite;

    public override int Version => 2;

    public override string Method => DsmApiMethods.Add;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => DsmApiNames.FileStationFavorite;

    public override int Version => 2;

    public override string Method => DsmApiMethods.Delete;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteEditParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => DsmApiNames.FileStationFavorite;

    public override int Version => 2;

    public override string Method => "edit";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
