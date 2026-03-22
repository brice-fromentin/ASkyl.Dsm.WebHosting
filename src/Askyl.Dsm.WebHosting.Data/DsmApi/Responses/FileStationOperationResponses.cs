using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Responses;

public class FileStationCreateFolderResponse : ApiResponseBase<FileStationFile>
{
}

public class FileStationRenameResponse : ApiResponseBase<FileStationFile>
{
}

public class FileStationDeleteResponse : ApiResponseBase<EmptyResponse>
{
}

public class FileStationCopyMoveResponse : ApiResponseBase<EmptyResponse>
{
}
