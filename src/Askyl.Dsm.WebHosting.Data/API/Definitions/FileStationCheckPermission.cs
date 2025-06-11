using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationCheckPermission : IGenericCloneable<FileStationCheckPermission>
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("overwrite")]
    public bool? Overwrite { get; set; }

    public FileStationCheckPermission Clone()
        => new()
        {
            Path = this.Path,
            Filename = this.Filename,
            Overwrite = this.Overwrite
        };
}
