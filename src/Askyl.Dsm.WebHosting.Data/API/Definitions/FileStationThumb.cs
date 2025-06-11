using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationThumb : IGenericCloneable<FileStationThumb>
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("size")]
    public string? Size { get; set; } = "medium"; // "small", "medium", "large", "original"

    [JsonPropertyName("rotate")]
    public int? Rotate { get; set; } = 0; // 0, 90, 180, 270

    public FileStationThumb Clone()
        => new()
        {
            Path = this.Path,
            Size = this.Size,
            Rotate = this.Rotate
        };
}
