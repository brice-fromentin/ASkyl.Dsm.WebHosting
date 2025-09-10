namespace Askyl.Dsm.WebHosting.Constants.API;

/// <summary>
/// Defines FileStation API specific parameters and constants.
/// </summary>
public static class FileStationDefaults
{

    #region Listing and Sorting

    /// <summary>
    /// Default additional fields to request when listing files and folders.
    /// </summary>
    public const string AdditionalPathSizeTimeFields = "[\"real_path\", \"size\", \"time\"]";

    public const string PatternAll = "*";

    public const string PatternDllsExes = "*.dll,*.exe";

    /// <summary>
    /// Default sort field for file listings.
    /// </summary>
    public const string SortByName = "name";

    /// <summary>
    /// Default sort direction for file listings.
    /// </summary>
    public const string SortDirectionAsc = "asc";

    /// <summary>
    /// Type identifier for all entries (files and directories).
    /// </summary>
    public const string TypeAll = "all";

    /// <summary>
    /// Type identifier for directory entries.
    /// </summary>
    public const string TypeDirectory = "dir";

    /// <summary>
    /// Type identifier for file entries.
    /// </summary>
    public const string TypeFile = "file";

    #endregion

    #region Compression

    /// <summary>
    /// Default compression level for archive operations.
    /// </summary>
    public const int DefaultCompressionLevel = 6;

    /// <summary>
    /// Minimum compression level for archive operations.
    /// </summary>
    public const int MinCompressionLevel = 0;

    /// <summary>
    /// Maximum compression level for archive operations.
    /// </summary>
    public const int MaxCompressionLevel = 9;

    /// <summary>
    /// Compression mode for adding files to archive.
    /// </summary>
    public const string CompressModeAdd = "add";

    /// <summary>
    /// Compression mode for updating files in archive.
    /// </summary>
    public const string CompressModeUpdate = "update";

    /// <summary>
    /// ZIP compression format.
    /// </summary>
    public const string CompressionFormatZip = "zip";

    /// <summary>
    /// 7-Zip compression format.
    /// </summary>
    public const string CompressionFormat7z = "7z";

    /// <summary>
    /// TAR compression format.
    /// </summary>
    public const string CompressionFormatTar = "tar";

    /// <summary>
    /// TGZ compression format.
    /// </summary>
    public const string CompressionFormatTgz = "tgz";

    /// <summary>
    /// TBZ compression format.
    /// </summary>
    public const string CompressionFormatTbz = "tbz";

    /// <summary>
    /// TXZ compression format.
    /// </summary>
    public const string CompressionFormatTxz = "txz";

    #endregion

    #region Virtual Folders

    /// <summary>
    /// Virtual folder type for all protocols.
    /// </summary>
    public const string VirtualFolderTypeAll = "all";

    /// <summary>
    /// Virtual folder type for CIFS protocol.
    /// </summary>
    public const string VirtualFolderTypeCifs = "cifs";

    /// <summary>
    /// Virtual folder type for NFS protocol.
    /// </summary>
    public const string VirtualFolderTypeNfs = "nfs";

    /// <summary>
    /// Virtual folder type for FTP protocol.
    /// </summary>
    public const string VirtualFolderTypeFtp = "ftp";

    /// <summary>
    /// Virtual folder type for SFTP protocol.
    /// </summary>
    public const string VirtualFolderTypeSftp = "sftp";

    /// <summary>
    /// Virtual folder type for WebDAV protocol.
    /// </summary>
    public const string VirtualFolderTypeWebdav = "webdav";

    #endregion
}
