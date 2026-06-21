using System.Text.Json.Serialization;
using ReverseProxyModel = Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy.ReverseProxy;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.AppPortal.ReverseProxy;

public class ReverseProxyListResponse : ApiResponseBase<ReverseProxyList>
{
}

public class ReverseProxyList
{
    [JsonPropertyName("entries")]
    public List<ReverseProxyModel> Entries { get; set; } = default!;
}
