using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Info;

public class InformationsQueryParameters()
    : ApiParametersBase<ApiInformationQuery>()
{
    public override string Name => ApiConstants.Info;

    public override int Version => 1;

    public override string Method => "query";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
