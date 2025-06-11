using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

public class FileStationSharingResponse : ApiResponseBase<FileStationSharingData>
{
}

public class FileStationSharingData
{
    [JsonPropertyName("links")]
    public List<FileStationSharingLink> Links { get; set; } = [];

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class FileStationSharingLink
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("link_owner")]
    public string LinkOwner { get; set; } = default!;

    [JsonPropertyName("path")]
    public string Path { get; set; } = default!;

    [JsonPropertyName("date_expired")]
    public long? DateExpired { get; set; }

    [JsonPropertyName("date_available")]
    public long? DateAvailable { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!; // "valid", "broken", "expired"

    [JsonPropertyName("has_password")]
    public bool HasPassword { get; set; }

    [JsonPropertyName("access_time")]
    public long? AccessTime { get; set; }

    [JsonPropertyName("isFolder")]
    public bool IsFolder { get; set; }
}

public class FileStationMd5Response : ApiResponseBase<FileStationMd5Data>
{
}

public class FileStationMd5Data
{
    [JsonPropertyName("md5")]
    public string Md5 { get; set; } = default!;
}

public class FileStationBackgroundTaskResponse : ApiResponseBase<FileStationBackgroundTaskData>
{
}

public class FileStationBackgroundTaskData
{
    [JsonPropertyName("tasks")]
    public List<FileStationBackgroundTask> Tasks { get; set; } = [];

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class FileStationBackgroundTask
{
    [JsonPropertyName("taskid")]
    public string TaskId { get; set; } = default!;

    [JsonPropertyName("api")]
    public string Api { get; set; } = default!;

    [JsonPropertyName("method")]
    public string Method { get; set; } = default!;

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("params")]
    public object? Params { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("time_started")]
    public long? TimeStarted { get; set; }

    [JsonPropertyName("time_finished")]
    public long? TimeFinished { get; set; }

    [JsonPropertyName("progress")]
    public FileStationTaskProgress? Progress { get; set; }

    [JsonPropertyName("error")]
    public object? Error { get; set; }
}

public class FileStationTaskProgress
{
    [JsonPropertyName("dest")]
    public string? Dest { get; set; }

    [JsonPropertyName("total")]
    public long? Total { get; set; }

    [JsonPropertyName("processed")]
    public long? Processed { get; set; }

    [JsonPropertyName("processing_path")]
    public string? ProcessingPath { get; set; }
}
