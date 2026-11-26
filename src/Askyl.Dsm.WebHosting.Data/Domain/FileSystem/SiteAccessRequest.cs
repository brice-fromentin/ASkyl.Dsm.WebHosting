namespace Askyl.Dsm.WebHosting.Data.Domain.FileSystem;

/// <summary>
/// Asks for DSM's http group to be given the access a site needs: Read on the application's folder, and passage
/// through each folder above it, up to the shared folder.
/// </summary>
/// <param name="ApplicationPath">The application's main file, as a share path (<c>/share/folder/App.dll</c>).</param>
/// <param name="ApplicationRealPath">The same file on its volume (<c>/volume1/share/folder/App.dll</c>).</param>
public record SiteAccessRequest(string ApplicationPath, string ApplicationRealPath);
