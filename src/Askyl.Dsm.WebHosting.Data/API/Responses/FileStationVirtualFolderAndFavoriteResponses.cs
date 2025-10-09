using System.Text.Json.Serialization;
using Askyl.Dsm.WebHosting.Data.API.Definitions.FileStation;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class FileStationVirtualFolderResponse : ApiResponseBase<FileStationVirtualFolderData>
{
}

public class FileStationVirtualFolderData
{
    [JsonPropertyName("folders")]
    public List<FileStationVirtualFolder> Folders { get; set; } = [];

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class FileStationVirtualFolder
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("additional")]
    public FileStationFileAdditional? Additional { get; set; }
}

public class FileStationFavoriteResponse : ApiResponseBase<FileStationFavoriteData>
{
}

public class FileStationFavoriteData
{
    [JsonPropertyName("favorites")]
    public List<FileStationFavorite> Favorites { get; set; } = [];

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class FileStationFavorite
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("status")]
    public FileStationFavoriteStatus? Status { get; set; }
}

public class FileStationFavoriteStatus
{
    [JsonPropertyName("digest")]
    public string? Digest { get; set; }

    [JsonPropertyName("serial")]
    public int? Serial { get; set; }
}
