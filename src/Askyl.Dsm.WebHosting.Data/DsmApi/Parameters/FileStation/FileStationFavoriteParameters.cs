using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;

public class FileStationFavoriteListParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => ApiNames.FileStationFavorite;

    public override int Version => 2;

    public override string Method => ApiMethods.List;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteAddParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => ApiNames.FileStationFavorite;

    public override int Version => 2;

    public override string Method => ApiMethods.Add;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteDeleteParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => ApiNames.FileStationFavorite;

    public override int Version => 2;

    public override string Method => ApiMethods.Delete;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}

public class FileStationFavoriteEditParameters(ApiInformationCollection informations) : ApiParametersBase<FileStationFavorite>(informations)
{
    public override string Name => ApiNames.FileStationFavorite;

    public override int Version => 2;

    public override string Method => "edit";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
