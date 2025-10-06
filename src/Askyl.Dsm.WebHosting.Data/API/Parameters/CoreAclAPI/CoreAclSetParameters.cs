using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Parameters.CoreAclAPI;

public class CoreAclSetParameters(ApiInformationCollection informations) : ApiParametersBase<CoreAclSet>(informations)
{
    public override string Name => DsmApiNames.CoreAcl;

    public override int Version => 1;

    public override string Method => "set";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
