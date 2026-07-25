namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Abstracts file system operations to enable unit testing without real file system access.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Reads all lines from the specified file.
    /// </summary>
    string[] ReadAllLines(string path);

    /// <summary>
    /// Opens the specified file for reading.
    /// </summary>
    Stream OpenRead(string path);

    /// <summary>
    /// Gets all files in the specified directory, optionally recursing into subdirectories.
    /// </summary>
    string[] GetFiles(string directoryPath, string searchPattern, bool recurseSubdirectories);
}

/// <summary>
/// Production implementation of <see cref="IFileReader"/> that accesses the real file system.
/// </summary>
public sealed class SystemFileReader : IFileReader
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string[] ReadAllLines(string path) => File.ReadAllLines(path);

    public Stream OpenRead(string path) => File.OpenRead(path);

    public string[] GetFiles(string directoryPath, string searchPattern, bool recurseSubdirectories)
        => Directory.GetFiles(directoryPath, searchPattern, recurseSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
}
