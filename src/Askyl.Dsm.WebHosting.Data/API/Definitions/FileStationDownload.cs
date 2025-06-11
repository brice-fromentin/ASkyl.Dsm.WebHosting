using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationDownload : IGenericCloneable<FileStationDownload>
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; set; } = [];

    [JsonPropertyName("mode")]
    public string? Mode { get; set; } = "open"; // "open" or "download"

    public FileStationDownload Clone()
        => new()
        {
            Paths = [.. this.Paths],
            Mode = this.Mode
        };
}
