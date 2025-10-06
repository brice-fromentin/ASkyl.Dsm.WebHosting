using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationFileAdditional
{
    [JsonPropertyName("real_path")]
    public string? RealPath { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("owner")]
    public FileStationOwner? Owner { get; set; }

    [JsonPropertyName("time")]
    public FileStationTime? Time { get; set; }

    [JsonPropertyName("perm")]
    public FileStationPermission? Permission { get; set; }

    [JsonPropertyName("mount_point_type")]
    public string? MountPointType { get; set; }

    [JsonPropertyName("type")]
    public string? FileType { get; set; }
}
