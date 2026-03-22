using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions.Core;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.InformationsAPI;

public class InformationsQueryParameters(ApiInformationCollection informations) : ApiParametersBase<ApiInformationQuery>(informations)
{
    public override string Name => DsmApiNames.Info;

    public override int Version => 1;

    public override string Method => "query";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
