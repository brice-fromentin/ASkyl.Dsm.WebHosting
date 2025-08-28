using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationShare : IGenericCloneable<FileStationShare>
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("isdir")]
    public bool IsDirectory { get; set; } = true; // Shares are always directories

    [JsonPropertyName("additional")]
    public FileStationShareAdditional? Additional { get; set; }

    public FileStationShare Clone()
        => new()
        {
            Name = this.Name,
            Path = this.Path,
            IsDirectory = this.IsDirectory,
            Additional = this.Additional?.Clone()
        };
}

public class FileStationShareAdditional : IGenericCloneable<FileStationShareAdditional>
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

    public FileStationShareAdditional Clone()
        => new()
        {
            RealPath = this.RealPath,
            Owner = this.Owner?.Clone(),
            Time = this.Time?.Clone(),
            Permission = this.Permission?.Clone(),
            MountPointType = this.MountPointType,
            VolumeStatus = this.VolumeStatus?.Clone()
        };
}

public class FileStationVolumeStatus : IGenericCloneable<FileStationVolumeStatus>
{
    [JsonPropertyName("freespace")]
    public long? FreeSpace { get; set; }

    [JsonPropertyName("totalspace")]
    public long? TotalSpace { get; set; }

    [JsonPropertyName("readonly")]
    public bool? ReadOnly { get; set; }

    public FileStationVolumeStatus Clone()
        => new()
        {
            FreeSpace = this.FreeSpace,
            TotalSpace = this.TotalSpace,
            ReadOnly = this.ReadOnly
        };
}
