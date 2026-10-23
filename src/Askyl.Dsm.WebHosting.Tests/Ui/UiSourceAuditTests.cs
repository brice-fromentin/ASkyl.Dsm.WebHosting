using System.Reflection;
using System.Text.RegularExpressions;
using Askyl.Dsm.WebHosting.Ui.Client.Extensions;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Blazilla;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Tests.Ui;

/// <summary>
/// Holds the UI sources to what the installed FluentUI really accepts, where the build cannot.
/// </summary>
/// <remarks>
/// FluentUI components capture unmatched attributes, so a parameter the library removed still compiles and
/// renders as a stray HTML attribute: no error, no warning. The FluentUI 5 compile spike measured it twice —
/// <c>SelectedOption</c> on <c>FluentSelect</c> and <c>Fill</c> on <c>FluentButton</c> — both green until an
/// audit script found them, and its first run here found three more already on 4.14.5
/// (docs/ai/fluentui-5-migration.md §3). Stylesheets and selectors fail the same way, silently.
/// </remarks>
public partial class UiSourceAuditTests
{
    /// <summary>
    /// Found by walking up from the test binaries; it marks the directory the projects live in.
    /// </summary>
    const string SolutionFileName = "Askyl.Dsm.WebHosting.slnx";

    const string BindPrefix = "@bind-";

    static readonly string[] AuditedProjects = ["Askyl.Dsm.WebHosting.Ui", "Askyl.Dsm.WebHosting.Ui.Client"];

    /// <summary>
    /// Assemblies whose components the sources use. A tag that resolves to none of them is plain HTML, a
    /// child-content slot such as <c>&lt;ChildContent&gt;</c>, or a C# generic argument, and is not audited.
    /// </summary>
    static readonly Assembly[] ComponentAssemblies =
    [
        typeof(FluentButton).Assembly,
        typeof(FluentValidator).Assembly,
        typeof(CultureManager).Assembly,
        typeof(EditForm).Assembly,
        typeof(RouteView).Assembly,
    ];

    /// <summary>
    /// Components that fetch a script from a public CDN at runtime, which the CSP refuses: FluentNumberInput
    /// loads IMask on first render, FluentSortableList loads Sortable (docs/ai/fluentui-5-migration.md §4.2).
    /// </summary>
    static readonly string[] CdnBackedComponents = ["FluentNumberInput", "FluentSortableList"];

    /// <summary>
    /// <c>MaskPattern</c> makes FluentTextInput load IMask from the same CDN.
    /// </summary>
    static readonly string[] CdnBackedParameters = ["MaskPattern"];

    /// <summary>
    /// Component types by the name a tag uses. A generic component is declared <c>Name`1</c> and written <c>Name</c>.
    /// </summary>
    static readonly Dictionary<string, Type> ComponentsByTag = ComponentAssemblies
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => !type.IsNested && !type.IsAbstract && typeof(IComponent).IsAssignableFrom(type))
        .GroupBy(type => type.Name.Split('`')[0], StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

    /// <summary>
    /// One component tag as written in a source file.
    /// </summary>
    sealed record AuditedTag(string Location, string Name, Type? Component, IReadOnlyList<string> Attributes);

    [GeneratedRegex(@"@\*.*?\*@|<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex Comments();

    [GeneratedRegex(@"<([A-Z][A-Za-z0-9]*)")]
    private static partial Regex TagStart();

    [GeneratedRegex(@"\.Show(Error|Info|Confirmation)Async\(")]
    private static partial Regex DirectMessageBoxCall();

    /// <summary>
    /// FluentUI 4 design tokens, which FluentUI 5 defines nowhere and aliases to nothing, and the elements and
    /// classes of FluentUI 4 components that no longer render. Comments are not matched: only <c>var(...)</c> uses.
    /// </summary>
    [GeneratedRegex(@"var\(\s*--(neutral|accent|type-ramp|design-unit|body-font)[a-z0-9-]*|fluent-(toolbar|text-field|number-field|dialog-header|dialog-footer)\b")]
    private static partial Regex FluentUi4OnlyReference();

    static string SourceRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException($"{SolutionFileName} not found above {AppContext.BaseDirectory}");
    }

    static IEnumerable<string> SourceFiles(string pattern)
    {
        var root = SourceRoot();
        var excluded = new[] { $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}" };

        return AuditedProjects
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(root, project), pattern, SearchOption.AllDirectories))
            .Where(path => !excluded.Any(segment => path.Contains(segment, StringComparison.Ordinal)));
    }

    static IReadOnlyList<AuditedTag> Tags()
    {
        var root = SourceRoot();

        return [.. SourceFiles("*.razor").SelectMany(path => ParseTags(Path.GetRelativePath(root, path), File.ReadAllText(path)))];
    }

    static IEnumerable<AuditedTag> ParseTags(string file, string source)
    {
        // Blanked rather than removed, so line numbers stay true
        var text = Comments().Replace(source, comment => new string(' ', comment.Length));

        foreach (Match match in TagStart().Matches(text))
        {
            var name = match.Groups[1].Value;
            var line = text.AsSpan(0, match.Index).Count('\n') + 1;
            var component = ComponentsByTag.GetValueOrDefault(name);

            yield return new($"{file}:{line}", name, component, ReadAttributeNames(text, match.Index + match.Length));
        }
    }

    /// <summary>
    /// Reads attribute names up to the end of the tag. Values are skipped with their parentheses balanced,
    /// so <c>Property="@(i => i.Name)"</c> or <c>Disabled="@(x == null)"</c> do not end the tag early.
    /// </summary>
    static List<string> ReadAttributeNames(string text, int position)
    {
        List<string> names = [];

        while (position < text.Length)
        {
            while (position < text.Length && Char.IsWhiteSpace(text[position]))
            {
                position++;
            }

            var start = position;

            while (position < text.Length && (Char.IsLetterOrDigit(text[position]) || text[position] is '@' or '-' or ':' or '.' or '_'))
            {
                position++;
            }

            // '>' or '/>' ends the tag; anything else means this was C#, such as List<FluentButton>
            if (position == start)
            {
                return names;
            }

            names.Add(text[start..position]);

            if (position < text.Length && text[position] == '=')
            {
                position = SkipValue(text, position + 1);
            }
        }

        return names;
    }

    static int SkipValue(string text, int position)
    {
        if (position >= text.Length || text[position] is not ('"' or '\''))
        {
            while (position < text.Length && !Char.IsWhiteSpace(text[position]) && text[position] is not ('>' or '/'))
            {
                position++;
            }

            return position;
        }

        var quote = text[position++];
        var depth = 0;

        for (; position < text.Length; position++)
        {
            switch (text[position])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                default:
                    if (text[position] == quote && depth <= 0)
                    {
                        return position + 1;
                    }

                    break;
            }
        }

        return position;
    }

    /// <summary>
    /// Whether an attribute reaches a declared parameter. Directives other than <c>@bind-</c> are Blazor's own;
    /// a lowercase or hyphenated name is an HTML attribute passed through on purpose, such as <c>class</c> or
    /// <c>aria-label</c>. Matching ignores case, as Blazor's parameter binding does.
    /// </summary>
    static bool IsDeclared(Type component, string attribute)
    {
        if (attribute.StartsWith('@'))
        {
            if (!attribute.StartsWith(BindPrefix, StringComparison.Ordinal))
            {
                return true;
            }

            attribute = attribute[BindPrefix.Length..].Split(':')[0];
        }
        else if (attribute.Contains('-') || attribute.All(Char.IsLower))
        {
            return true;
        }

        var parameters = component
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.IsDefined(typeof(ParameterAttribute), inherit: true))
            .Select(property => property.Name)
            .Concat(component.IsGenericTypeDefinition ? component.GetGenericArguments().Select(argument => argument.Name) : []);

        return parameters.Contains(attribute, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheAudit_FindsTagsFromEveryAssemblyItCovers()
    {
        // The guard that keeps the rest of this class honest: a parser that silently found nothing, or a
        // root that pointed at the wrong directory, would pass every assertion below in green.
        var covered = Tags().Where(tag => tag.Component is not null).Select(tag => tag.Component!.Assembly).ToHashSet();

        Assert.All(ComponentAssemblies, assembly => Assert.Contains(assembly, covered));
    }

    [Fact]
    public void EveryAttribute_IsAParameterOfItsComponent()
    {
        string[] undeclared = [.. Tags()
            .Where(tag => tag.Component is not null)
            .SelectMany(tag => tag.Attributes.Where(attribute => !IsDeclared(tag.Component!, attribute)).Select(attribute => $"{tag.Location} <{tag.Name}> {attribute}"))];

        AssertNone(undeclared);
    }

    [Fact]
    public void NoComponent_LoadsAScriptFromACdn()
    {
        string[] offending = [.. Tags()
            .Where(tag => CdnBackedComponents.Contains(tag.Name, StringComparer.Ordinal) || tag.Attributes.Any(attribute => CdnBackedParameters.Contains(attribute, StringComparer.Ordinal)))
            .Select(tag => $"{tag.Location} <{tag.Name}>")];

        AssertNone(offending);
    }

    [Fact]
    public void MessageBoxes_AreOnlyShownThroughThePlainTextHelpers()
    {
        // FluentUI 5 throws on any angle bracket in a message box's text; the helpers encode it first
        var helpers = $"{nameof(DialogServiceExtensions)}.cs";
        var root = SourceRoot();

        string[] direct = [.. SourceFiles("*.razor").Concat(SourceFiles("*.cs"))
            .Where(path => !String.Equals(Path.GetFileName(path), helpers, StringComparison.Ordinal))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (Path: path, Line: line, Number: index + 1)))
            .Where(entry => DirectMessageBoxCall().IsMatch(entry.Line))
            .Select(entry => $"{Path.GetRelativePath(root, entry.Path)}:{entry.Number}")];

        AssertNone(direct);
    }

    [Fact]
    public void Sources_ReferenceNoTokenOrElementOnlyFluentUi4Had()
    {
        var root = SourceRoot();

        string[] stale = [.. SourceFiles("*.razor").Concat(SourceFiles("*.css")).Concat(SourceFiles("*.js"))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (Path: path, Line: line, Number: index + 1)))
            .SelectMany(entry => FluentUi4OnlyReference().Matches(entry.Line).Select(match => $"{Path.GetRelativePath(root, entry.Path)}:{entry.Number} {match.Value}"))];

        AssertNone(stale);
    }

    /// <summary>
    /// Lists every finding in full; <c>Assert.Empty</c> truncates each entry before the part that matters.
    /// </summary>
    static void AssertNone(string[] findings) => Assert.True(findings.Length == 0, String.Join(Environment.NewLine, findings));
}
