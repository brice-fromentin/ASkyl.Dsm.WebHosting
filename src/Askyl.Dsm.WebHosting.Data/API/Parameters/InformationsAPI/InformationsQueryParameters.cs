using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.InformationsAPI;

public class InformationsQueryParameters(ApiInformationCollection informations) : ApiParametersBase<ApiInformationQuery>(informations)
{
    public override string Name => DsmDefaults.DsmApiInfo;

    public override int Version => 1;

    public override string Method => "query";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
