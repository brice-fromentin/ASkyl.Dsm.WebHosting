using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;

public class FileStationFavoriteListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationFavorite;

    public override int Version => 2;

    public override string Method => "list";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteAddParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationFavorite;

    public override int Version => 2;

    public override string Method => "add";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationFavorite;

    public override int Version => 2;

    public override string Method => "delete";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteEditParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => DsmDefaults.DsmApiFileStationFavorite;

    public override int Version => 2;

    public override string Method => "edit";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
