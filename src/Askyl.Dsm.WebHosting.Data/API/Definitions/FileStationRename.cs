using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationRename : IGenericCloneable<FileStationRename>
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("additional")]
    public string? Additional { get; set; }

    public FileStationRename Clone()
        => new()
        {
            Path = this.Path,
            Name = this.Name,
            Additional = this.Additional
        };
}
