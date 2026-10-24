using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Tests.Ui;

/// <summary>
/// Holds the WebAssembly the browser downloads to the assemblies the build compiled against.
/// </summary>
/// <remarks>
/// The SDK converts a DLL to webcil only when the DLL is newer than the existing output, and the output name
/// carries no version (<c>ConvertDllsToWebCil</c>, WebAssembly pack 10.0.13). A package upgrade whose DLL
/// predates the last local build therefore keeps the old webcil. After the move to FluentUI 5.0.1 the browser
/// still received 4.14.5 and failed on <c>DefaultValues</c> at startup, while every other test passed: tests load
/// the DLLs, never the webcil. <c>dotnet clean</c> clears it.
/// </remarks>
public class WebAssemblyOutputTests
{
    const string ClientProject = "Askyl.Dsm.WebHosting.Ui.Client";

    const string WebcilExtension = ".wasm";

    const string DllExtension = ".dll";

    /// <summary>
    /// The start of the metadata root, which webcil keeps intact inside its WebAssembly wrapper.
    /// </summary>
    static readonly byte[] MetadataSignature = "BSJB"u8.ToArray();

    /// <summary>
    /// Reads the version from the first signature that parses as a metadata root. Assemblies that read metadata
    /// themselves, such as System.Reflection.Metadata, also carry the signature as a literal in their code.
    /// </summary>
    static Version WebcilVersion(string path)
    {
        var bytes = File.ReadAllBytes(path);

        for (var offset = bytes.AsSpan().IndexOf(MetadataSignature); offset >= 0;)
        {
            try
            {
                using var provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(bytes, offset, bytes.Length - offset));

                return provider.GetMetadataReader().GetAssemblyDefinition().Version;
            }
            catch (BadImageFormatException)
            {
                var next = bytes.AsSpan(offset + 1).IndexOf(MetadataSignature);
                offset = next < 0 ? -1 : offset + 1 + next;
            }
        }

        throw new BadImageFormatException($"No metadata root in {path}");
    }

    static Version DllVersion(string path)
    {
        using var reader = new PEReader(File.OpenRead(path));

        return reader.GetMetadataReader().GetAssemblyDefinition().Version;
    }

    [Fact]
    public void EveryWebcilAssembly_IsTheVersionTheTestsCompiledAgainst()
    {
        // The test binaries sit in bin/<configuration>/<framework>/, which names the client's matching folders.
        // The client's bin holds the DLLs it compiled against; the test's own bin resolves some packages differently.
        var output = new DirectoryInfo(AppContext.BaseDirectory);
        var client = Path.Combine(UiSourceAuditTests.SourceRoot(), ClientProject);
        var webcil = Path.Combine(client, "obj", output.Parent!.Name, output.Name, "webcil");
        var compiled = Path.Combine(client, "bin", output.Parent.Name, output.Name);

        Assert.True(Directory.Exists(webcil), $"{webcil} not found: build the solution before testing");

        var compared = Directory.EnumerateFiles(webcil, $"*{WebcilExtension}", SearchOption.AllDirectories)
            .Select(path => (Webcil: path, Dll: Path.ChangeExtension(Path.Combine(compiled, Path.GetRelativePath(webcil, path)), DllExtension)))
            .Where(pair => File.Exists(pair.Dll))
            .Select(pair => (Name: Path.GetFileNameWithoutExtension(pair.Dll), Served: WebcilVersion(pair.Webcil), Compiled: DllVersion(pair.Dll)))
            .ToList();

        // Guard: a lookup that matched nothing would pass in green; FluentUI is the case this test exists for
        Assert.Contains(compared, entry => entry.Name == typeof(FluentButton).Assembly.GetName().Name);

        string[] stale = [.. compared.Where(entry => entry.Served != entry.Compiled).Select(entry => $"{entry.Name}: served {entry.Served}, compiled against {entry.Compiled} — run dotnet clean")];

        Assert.True(stale.Length == 0, String.Join(Environment.NewLine, stale));
    }
}
