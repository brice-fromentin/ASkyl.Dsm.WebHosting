using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class FileStationInfoResponse : ApiResponseBase<FileStationInfoData>
{
}

public class FileStationInfoData
{
    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("is_manager")]
    public bool IsManager { get; set; }

    [JsonPropertyName("support_sharing")]
    public bool SupportSharing { get; set; }

    [JsonPropertyName("support_virtual_protocol")]
    public List<string>? SupportVirtualProtocol { get; set; }

    [JsonPropertyName("support_extract")]
    public bool SupportExtract { get; set; }

    [JsonPropertyName("support_file_request")]
    public bool SupportFileRequest { get; set; }
}

public class FileStationCheckPermissionResponse : ApiResponseBase<FileStationCheckPermissionData>
{
}

public class FileStationCheckPermissionData
{
    [JsonPropertyName("result")]
    public bool Result { get; set; }
}

public class FileStationDirSizeResponse : ApiResponseBase<FileStationDirSizeData>
{
}

public class FileStationDirSizeData
{
    [JsonPropertyName("taskid")]
    public string? TaskId { get; set; }

    [JsonPropertyName("num_dir")]
    public int? NumDir { get; set; }

    [JsonPropertyName("num_file")]
    public int? NumFile { get; set; }

    [JsonPropertyName("total_size")]
    public long? TotalSize { get; set; }

    [JsonPropertyName("finished")]
    public bool Finished { get; set; }
}
