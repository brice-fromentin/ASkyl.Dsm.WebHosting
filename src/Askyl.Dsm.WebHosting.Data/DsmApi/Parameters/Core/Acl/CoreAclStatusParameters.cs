using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.Acl;

public class CoreAclStatusParameters(CoreAclTask? entry = null)
    : ApiParametersBase<CoreAclTask>(entry)
{
    public override string Name => ApiConstants.CoreAcl;

    public override int Version => 1;

    public override string Method => ApiConstants.MethodStatus;

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
