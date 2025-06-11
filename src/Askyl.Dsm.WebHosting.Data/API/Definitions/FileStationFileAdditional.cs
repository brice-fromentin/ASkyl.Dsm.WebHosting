using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Definitions;

public class FileStationFileAdditional : IGenericCloneable<FileStationFileAdditional>
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

    public FileStationFileAdditional Clone()
        => new()
        {
            RealPath = this.RealPath,
            Size = this.Size,
            Owner = this.Owner?.Clone(),
            Time = this.Time?.Clone(),
            Permission = this.Permission?.Clone(),
            MountPointType = this.MountPointType,
            FileType = this.FileType
        };
}
