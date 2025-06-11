using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationDirSize : IGenericCloneable<FileStationDirSize>
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; set; } = [];

    public FileStationDirSize Clone()
        => new()
        {
            Paths = [.. this.Paths]
        };
}
