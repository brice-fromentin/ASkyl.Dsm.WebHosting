using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.CoreAcl;

public class CoreAclSetParameters(ApiInformationCollection informations) : ApiParametersBase<CoreAclSet>(informations)
{
    public override string Name => ApiNames.CoreAcl;

    public override int Version => 1;

    public override string Method => "set";

    public override SerializationFormats SerializationFormat => SerializationFormats.Form;
}
