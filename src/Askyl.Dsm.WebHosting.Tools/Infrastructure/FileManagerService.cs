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
    private static string SanitizePathSegment(string? name, string paramName, bool allowEmpty)
    {
        ArgumentNullException.ThrowIfNull(name, paramName);

        if (name.Length == 0)
        {
            if (allowEmpty)
            {
                return String.Empty;
            }

            throw new ArgumentException("Value cannot be empty", paramName);
        }

        if (name.Trim().Length == 0)
        {
            throw new ArgumentException("Value cannot be whitespace", paramName);
        }

        var sanitized = Path.GetFileName(name);

        if (String.Equals(sanitized, String.Empty, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid value: contains only path separators", paramName);
        }

        return sanitized;
    }

    /// <inheritdoc/>
    public string BaseDirectory => AppContext.BaseDirectory;

    /// <inheritdoc/>
    public void Initialize()
    {
        logger.LogInformation("Initializing FileManager with base path: {BasePath}", String.IsNullOrEmpty(rootPath) ? BaseDirectory : Path.Combine(BaseDirectory, rootPath));

        // Create default directories
        GetDirectory(InfrastructureConstants.Downloads);
        GetDirectory(InfrastructureConstants.TempDirectory);

        logger.LogInformation("FileManager initialized successfully");
    }

    /// <inheritdoc/>
    public string GetDirectory(string name)
    {
        var sanitized = SanitizePathSegment(name, nameof(name), true);
        var path = Path.Combine(BaseDirectory, rootPath, sanitized);

        logger.LogDebug("Ensuring directory exists: {DirectoryPath}", path);
        Directory.CreateDirectory(path);

        return path;
    }

    /// <inheritdoc/>
    public void DeleteDirectory(string name)
    {
        var sanitized = SanitizePathSegment(name, nameof(name), false);
        var path = Path.Combine(BaseDirectory, rootPath, sanitized);

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
        var sanitizedFile = SanitizePathSegment(file, nameof(file), false);
        var path = GetDirectory(directory);
        var fullPath = Path.Combine(path, sanitizedFile);

        logger.LogDebug("Getting full file path: {FullPath}", fullPath);
        return fullPath;
    }
}
