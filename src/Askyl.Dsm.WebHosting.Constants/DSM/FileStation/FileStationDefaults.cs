namespace Askyl.Dsm.WebHosting.Constants.DSM.FileStation;

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

    /// <summary>
    /// Pattern to match all files.
    /// </summary>
    public const string PatternAll = "*";

    /// <summary>
    /// Pattern to match DLL and EXE files.
    /// </summary>
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
    /// Default compression level for archive operations (0-9).
    /// </summary>
    public const int DefaultCompressionLevel = 6;

    /// <summary>
    /// Compression mode for adding files to archive.
    /// </summary>
    public const string CompressModeAdd = "add";

    /// <summary>
    /// ZIP compression format (only supported format).
    /// </summary>
    public const string CompressionFormatZip = "zip";

    #endregion

    #region Virtual Folders

    /// <summary>
    /// Virtual folder type for all protocols.
    /// </summary>
    public const string VirtualFolderTypeAll = "all";

    #endregion
}
