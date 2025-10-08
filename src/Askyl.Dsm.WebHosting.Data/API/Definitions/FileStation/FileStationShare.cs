using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.SourceGenerators;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

[GenerateClone]
public partial class FileStationShare
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("isdir")]
    public bool IsDirectory { get; set; } = true; // Shares are always directories

    [JsonPropertyName("additional")]
    public FileStationShareAdditional? Additional { get; set; }
}

[GenerateClone]
public partial class FileStationShareAdditional
{
    [JsonPropertyName("real_path")]
    public string? RealPath { get; set; }

    [JsonPropertyName("owner")]
    public FileStationOwner? Owner { get; set; }

    [JsonPropertyName("time")]
    public FileStationTime? Time { get; set; }

    [JsonPropertyName("perm")]
    public FileStationPermission? Permission { get; set; }

    [JsonPropertyName("mount_point_type")]
    public string? MountPointType { get; set; }

    [JsonPropertyName("volume_status")]
    public FileStationVolumeStatus? VolumeStatus { get; set; }
}

[GenerateClone]
public partial class FileStationVolumeStatus
{
    [JsonPropertyName("freespace")]
    public long? FreeSpace { get; set; }

    [JsonPropertyName("totalspace")]
    public long? TotalSpace { get; set; }

    [JsonPropertyName("readonly")]
    public bool? ReadOnly { get; set; }
}
