using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Detects the required .NET runtime framework from assembly files.
/// </summary>
public interface IAssemblyRuntimeDetector
{
    /// <summary>
    /// Detects the target framework of a .NET assembly file.
    /// </summary>
    /// <param name="assemblyPath">The absolute path to the assembly file.</param>
    /// <returns>Detection result, or null if the file is not a .NET assembly or cannot be read.</returns>
    AssemblyRuntimeInfo? Detect(string assemblyPath);
}
