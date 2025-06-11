using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationMd5 : IGenericCloneable<FileStationMd5>
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = default!;

    public FileStationMd5 Clone()
        => new()
        {
            FilePath = this.FilePath
        };
}
