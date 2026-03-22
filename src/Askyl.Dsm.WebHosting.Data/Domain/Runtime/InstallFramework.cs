namespace Askyl.Dsm.WebHosting.Data.Domain.Runtime;

/// <summary>
/// Request model for framework installation operations.
/// </summary>
public record InstallFramework(string Version, string Channel);
