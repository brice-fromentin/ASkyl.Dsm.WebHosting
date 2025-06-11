using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class FileStationSearchResponse : ApiResponseBase<FileStationSearchData>
{
}

public class FileStationSearchData
{
    [JsonPropertyName("files")]
    public List<FileStationFile> Files { get; set; } = [];

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("taskid")]
    public string? TaskId { get; set; }

    [JsonPropertyName("finished")]
    public bool Finished { get; set; }
}
