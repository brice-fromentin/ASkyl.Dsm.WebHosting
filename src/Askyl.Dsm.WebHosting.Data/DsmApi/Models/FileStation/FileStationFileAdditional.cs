using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

public record FileStationFileAdditional
{
    [JsonPropertyName("real_path")]
    public string? RealPath { get; init; }

    [JsonPropertyName("size")]
    public long? Size { get; init; }

    [JsonPropertyName("owner")]
    public FileStationOwner? Owner { get; init; }

    [JsonPropertyName("time")]
    public FileStationTime? Time { get; init; }

    [JsonPropertyName("perm")]
    public FileStationPermission? Permission { get; init; }

    [JsonPropertyName("mount_point_type")]
    public string? MountPointType { get; init; }

    [JsonPropertyName("type")]
    public string? FileType { get; init; }
}
