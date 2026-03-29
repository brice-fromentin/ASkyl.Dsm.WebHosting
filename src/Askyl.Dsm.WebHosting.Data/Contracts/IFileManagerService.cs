namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Provides file system operations for managing application directories and files.
/// </summary>
public interface IFileManagerService
{
    /// <summary>
    /// Initializes the file manager by creating default directories (downloads, temp) if they don't exist.
    /// The root path is configured via constructor parameter.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Gets or creates a directory with the specified name.
    /// Returns the full path to the directory.
    /// </summary>
    /// <param name="name">The directory name relative to the root.</param>
    /// <returns>The full path to the directory.</returns>
    string GetDirectory(string name);

    /// <summary>
    /// Deletes a directory with the specified name if it exists.
    /// </summary>
    /// <param name="name">The directory name relative to the root.</param>
    void DeleteDirectory(string name);

    /// <summary>
    /// Gets the full path for a file within a specific directory.
    /// Ensures the directory exists before returning the path.
    /// </summary>
    /// <param name="directory">The directory name relative to the root.</param>
    /// <param name="file">The file name.</param>
    /// <returns>The full path to the file.</returns>
    string GetFullName(string directory, string file);

    /// <summary>
    /// Gets the base directory for the application.
    /// </summary>
    string BaseDirectory { get; }
}
