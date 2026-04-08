using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Injectable file manager service for managing application directories and files.
/// Replaces the static FileManager class with DI-based implementation.
/// </summary>
public sealed class FileManagerService(ILogger<FileManagerService> logger, string rootPath = "") : IFileManagerService
{
    private readonly string _rootPath = rootPath;

    /// <inheritdoc/>
    public string BaseDirectory => AppContext.BaseDirectory;

    /// <inheritdoc/>
    public void Initialize()
    {
        logger.LogInformation("Initializing FileManager with base path: {BasePath}", String.IsNullOrEmpty(_rootPath) ? BaseDirectory : Path.Combine(BaseDirectory, _rootPath));

        // Create default directories
        GetDirectory(InfrastructureConstants.Downloads);
        GetDirectory(InfrastructureConstants.TempDirectory);

        logger.LogInformation("FileManager initialized successfully");
    }

    /// <inheritdoc/>
    public string GetDirectory(string name)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Directory name cannot be empty", nameof(name));
        }

        // Prevent path traversal - extract only the file/directory name
        var sanitized = Path.GetFileName(name);

        if (String.Equals(sanitized, String.Empty, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid directory name: contains only path separators", nameof(name));
        }

        var path = Path.Combine(BaseDirectory, _rootPath, sanitized);

        logger.LogDebug("Ensuring directory exists: {DirectoryPath}", path);
        Directory.CreateDirectory(path);

        return path;
    }

    /// <inheritdoc/>
    public void DeleteDirectory(string name)
    {
        var path = Path.Combine(BaseDirectory, _rootPath, name);

        if (Directory.Exists(path))
        {
            logger.LogInformation("Deleting directory: {DirectoryPath}", path);
            Directory.Delete(path, true);
        }
        else
        {
            logger.LogDebug("Directory does not exist, skipping deletion: {DirectoryPath}", path);
        }
    }

    /// <inheritdoc/>
    public string GetFullName(string directory, string file)
    {
        var path = GetDirectory(directory);
        var fullPath = Path.Combine(path, file);

        logger.LogDebug("Getting full file path: {FullPath}", fullPath);
        return fullPath;
    }
}
