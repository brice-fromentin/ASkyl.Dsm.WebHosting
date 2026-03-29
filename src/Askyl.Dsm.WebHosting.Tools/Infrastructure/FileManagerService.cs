using Microsoft.Extensions.Logging;

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;

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

    /// <summary>
    /// Default directory name for temporary files.
    /// </summary>
    private const string Temp = "temp";

    /// <inheritdoc/>
    public void Initialize()
    {
        logger.LogInformation("Initializing FileManager with base path: {BasePath}", String.IsNullOrEmpty(_rootPath) ? BaseDirectory : Path.Combine(BaseDirectory, _rootPath));

        // Create default directories
        GetDirectory(InfrastructureConstants.Downloads);
        GetDirectory(Temp);

        logger.LogInformation("FileManager initialized successfully");
    }

    /// <inheritdoc/>
    public string GetDirectory(string name)
    {
        var path = Path.Combine(BaseDirectory, _rootPath, name);

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
