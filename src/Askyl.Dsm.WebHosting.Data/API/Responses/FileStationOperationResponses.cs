using Askyl.Dsm.WebHosting.Data.API.Definitions;

namespace Askyl.Dsm.WebHosting.Data.API.Responses;

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
