namespace Askyl.Dsm.WebHosting.Data.Domain.Licensing;

/// <summary>
/// Represents a software license with name and content.
/// </summary>
/// <param name="Name">The name of the license.</param>
/// <param name="Content">The full text content of the license.</param>
public record LicenseInfo(string Name, string Content);
