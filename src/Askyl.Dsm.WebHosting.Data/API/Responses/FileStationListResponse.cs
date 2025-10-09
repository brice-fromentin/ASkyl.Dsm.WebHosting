using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class FileStationListResponse : ApiResponseBase<FileStationListData>
{
}

public class FileStationListData
{
    [JsonPropertyName("files")]
    public List<FileStationFile> Files { get; set; } = [];

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
