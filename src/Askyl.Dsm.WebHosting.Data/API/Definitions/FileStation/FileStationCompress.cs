using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

[GenerateClone]
public partial class FileStationCompress
{
    [JsonPropertyName("path")]
    public List<string> Paths { get; set; } = [];

    [JsonPropertyName("dest_file_path")]
    public string DestFilePath { get; set; } = default!;

    [JsonPropertyName("level")]
    public int? Level { get; set; } = FileStationDefaults.DefaultCompressionLevel; // Compression level 0-9

    [JsonPropertyName("mode")]
    public string? Mode { get; set; } = FileStationDefaults.CompressModeAdd;

    [JsonPropertyName("format")]
    public string? Format { get; set; } = FileStationDefaults.CompressionFormatZip;

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
