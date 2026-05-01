using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.DSM.FileStation;


namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;


public record FileStationCompress
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; init; } = [];

    [JsonPropertyName("dest_file_path")]
    public string DestFilePath { get; init; } = default!;

    [JsonPropertyName("level")]
    public int? Level { get; init; } = FileStationDefaults.DefaultCompressionLevel; // Compression level 0-9

    [JsonPropertyName("mode")]
    public string? Mode { get; init; } = FileStationDefaults.CompressModeAdd;

    [JsonPropertyName("format")]
    public string? Format { get; init; } = FileStationDefaults.CompressionFormatZip;

    [JsonPropertyName("password")]
    public string? Password { get; init; }
}
