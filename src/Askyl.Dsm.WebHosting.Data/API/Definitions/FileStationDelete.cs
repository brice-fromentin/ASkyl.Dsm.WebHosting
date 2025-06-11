using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationDelete : IGenericCloneable<FileStationDelete>
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; set; } = [];

    [JsonPropertyName("accurate_progress")]
    public bool? AccurateProgress { get; set; } = false;

    [JsonPropertyName("recursive")]
    public bool? Recursive { get; set; } = true;

    public FileStationDelete Clone()
        => new()
        {
            Paths = [.. this.Paths],
            AccurateProgress = this.AccurateProgress,
            Recursive = this.Recursive
        };
}
