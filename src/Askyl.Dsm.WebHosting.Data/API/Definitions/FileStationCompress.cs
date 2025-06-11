using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationCompress : IGenericCloneable<FileStationCompress>
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; set; } = [];

    [JsonPropertyName("dest_file_path")]
    public string DestFilePath { get; set; } = default!;

    [JsonPropertyName("level")]
    public int? Level { get; set; } = 6; // Compression level 0-9

    [JsonPropertyName("mode")]
    public string? Mode { get; set; } = "add"; // "add" or "update"

    [JsonPropertyName("format")]
    public string? Format { get; set; } = "zip"; // "zip", "7z", "tar", "tgz", "tbz", "txz"

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    public FileStationCompress Clone()
        => new()
        {
            Paths = [.. this.Paths],
            DestFilePath = this.DestFilePath,
            Level = this.Level,
            Mode = this.Mode,
            Format = this.Format,
            Password = this.Password
        };
}
