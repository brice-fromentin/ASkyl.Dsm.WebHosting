using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Infrastructure;

/// <summary>
/// Injectable file manager service for managing application directories and files.
/// Replaces the static FileManager class with DI-based implementation.
/// </summary>
public sealed class FileManagerService(ILogger<ILogFileManagerService> logger, string rootPath = "") : IFileManagerService
{
    private readonly string _normalizedRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rootPath));

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

    private static string SanitizeSubdirectoryPath(string? name, string paramName)
    {
        ArgumentNullException.ThrowIfNull(name, paramName);

        if (name.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty", paramName);
        }

        if (name.Trim().Length == 0)
        {
            throw new ArgumentException("Value cannot be whitespace", paramName);
        }

        // Prevent directory traversal
        var segments = name.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (segment.Equals("..", StringComparison.OrdinalIgnoreCase) || segment.Equals(".", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid path segment '{segment}' in '{name}'", paramName);
            }
        }

        return name;
    }

    /// <inheritdoc/>
    public void Initialize()
    {
        logger.FileManagerInitializing(_normalizedRootPath);

        // Create default directories
        GetDirectory(InfrastructureConstants.Downloads);
        GetDirectory(InfrastructureConstants.TempDirectory);

        logger.FileManagerInitialized();
    }

    /// <inheritdoc/>
    public string GetDirectory(string name)
    {
        var sanitized = SanitizePathSegment(name, nameof(name), true);
        var path = Path.Combine(_normalizedRootPath, sanitized);

        logger.EnsuringDirectoryExists(path);
        Directory.CreateDirectory(path);

        return path;
    }

    /// <inheritdoc/>
    public void DeleteDirectory(string name)
    {
        var sanitized = SanitizeSubdirectoryPath(name, nameof(name));
        var path = Path.Combine(_normalizedRootPath, sanitized);

        if (Directory.Exists(path))
        {
            logger.DeletingDirectory(path);
            Directory.Delete(path, true);
        }

        else
        {
            logger.DirectoryNotFoundSkippingDeletion(path);
        }
    }

    /// <inheritdoc/>
    public string GetFullName(string directory, string file)
    {
        var sanitizedFile = SanitizePathSegment(file, nameof(file), false);
        var path = GetDirectory(directory);
        var fullPath = Path.Combine(path, sanitizedFile);

        logger.GettingFullPath(fullPath);
        return fullPath;
    }
}
