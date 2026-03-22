namespace Askyl.Dsm.WebHosting.Data;

/// <summary>
/// Represents a software license with name and content.
/// </summary>
/// <param name="name">The name of the license.</param>
/// <param name="content">The full text content of the license.</param>
public record LicenseInfo(string Name, string Content);
