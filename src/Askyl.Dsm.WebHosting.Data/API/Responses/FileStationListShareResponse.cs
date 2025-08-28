using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class FileStationListShareResponse : ApiResponseBase<FileStationListShareData>
{
}

public class FileStationListShareData
{
    [JsonPropertyName("shares")]
    public List<FileStationShare> Shares { get; set; } = [];

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
