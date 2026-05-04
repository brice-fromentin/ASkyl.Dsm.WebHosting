using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationShare
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; init; } = default!;

    [JsonPropertyName("isdir")]
    public bool IsDirectory { get; init; } = true; // Shares are always directories

    [JsonPropertyName("additional")]
    public FileStationShareAdditional? Additional { get; init; }
}

public record FileStationShareAdditional
{
    [JsonPropertyName("real_path")]
    public string? RealPath { get; init; }

    [JsonPropertyName("owner")]
    public FileStationOwner? Owner { get; init; }

    [JsonPropertyName("time")]
    public FileStationTime? Time { get; init; }

    [JsonPropertyName("perm")]
    public FileStationPermission? Permission { get; init; }

    [JsonPropertyName("mount_point_type")]
    public string? MountPointType { get; init; }

    [JsonPropertyName("volume_status")]
    public FileStationVolumeStatus? VolumeStatus { get; init; }
}

public record FileStationVolumeStatus
{
    [JsonPropertyName("freespace")]
    public long? FreeSpace { get; init; }

    [JsonPropertyName("totalspace")]
    public long? TotalSpace { get; init; }

    [JsonPropertyName("readonly")]
    public bool? ReadOnly { get; init; }
}
