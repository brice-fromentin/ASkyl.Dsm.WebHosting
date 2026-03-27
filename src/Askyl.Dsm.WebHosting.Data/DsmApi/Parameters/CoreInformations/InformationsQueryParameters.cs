using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.CoreInformations;

public class InformationsQueryParameters(ApiInformationCollection informations) : ApiParametersBase<ApiInformationQuery>(informations)
{
    public override string Name => ApiNames.Info;

    public override int Version => 1;

    public override string Method => "query";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
