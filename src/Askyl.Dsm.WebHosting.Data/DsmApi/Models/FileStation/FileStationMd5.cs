using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationMd5
{
    [JsonPropertyName("file_path")]
    public string FilePath { get; init; } = default!;
}
