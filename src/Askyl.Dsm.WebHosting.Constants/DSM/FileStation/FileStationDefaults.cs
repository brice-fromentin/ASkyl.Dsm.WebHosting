namespace Askyl.Dsm.WebHosting.Constants.DSM.FileStation;

/// <summary>
/// Defines FileStation API specific parameters and constants.
/// </summary>
public static class FileStationDefaults
{
    /// <summary>
    /// Separator of DSM share paths (<c>/share/folder</c>) and of the real paths behind them, whatever the host.
    /// </summary>
    public const char PathSeparator = '/';

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
}
