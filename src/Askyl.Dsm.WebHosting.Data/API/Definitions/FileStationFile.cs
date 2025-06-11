using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationFile : IGenericCloneable<FileStationFile>
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!; // "file" or "dir"

    [JsonPropertyName("isdir")]
    public bool IsDirectory { get; set; }

    [JsonPropertyName("children")]
    public List<FileStationFile>? Children { get; set; }

    [JsonPropertyName("additional")]
    public FileStationFileAdditional? Additional { get; set; }

    public FileStationFile Clone()
        => new()
        {
            Name = this.Name,
            Path = this.Path,
            Type = this.Type,
            IsDirectory = this.IsDirectory,
            Children = this.Children?.Select(x => x.Clone()).ToList(),
            Additional = this.Additional?.Clone()
        };
}
