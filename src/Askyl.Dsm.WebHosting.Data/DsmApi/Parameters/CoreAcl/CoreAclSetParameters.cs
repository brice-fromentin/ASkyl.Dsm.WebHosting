using Askyl.Dsm.WebHosting.Constants.DSM.API;
// Removed - using same namespace
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.CoreAcl;

public class CoreAclSetParameters(ApiInformationCollection informations) : ApiParametersBase<CoreAclSet>(informations)
{
    public override string Name => ApiNames.CoreAcl;

    public override int Version => 1;

    public override string Method => "set";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
