namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class ReverseProxyUuids : List<Guid>, IGenericCloneable<ReverseProxyUuids>
{
    public ReverseProxyUuids Clone()
        => [.. this];
}
