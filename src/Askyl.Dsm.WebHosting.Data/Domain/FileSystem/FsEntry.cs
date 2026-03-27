namespace Askyl.Dsm.WebHosting.Data.Domain.FileSystem;

/// <summary>
/// Represents a file system item (file or folder) with basic information.
/// Used for data transfer between server and client layers.
/// </summary>
/// <param name="Path">The full path to the item.</param>
/// <param name="Name">The display name of the item.</param>
/// <param name="IsDirectory">True if the item is a directory, false otherwise.</param>
/// <param name="RealPath">The resolved real path (for mounted drives/external storage).</param>
/// <param name="Size">The size of the file in bytes (null for directories).</param>
/// <param name="Modified">The modification time of the item.</param>
public record FsEntry(string Path, string Name, bool IsDirectory, string RealPath, long? Size, DateTime Modified);
