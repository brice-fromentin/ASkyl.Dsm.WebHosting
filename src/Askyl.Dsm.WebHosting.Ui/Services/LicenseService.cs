using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Ui.Models;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface ILicenseService
{
    IReadOnlyCollection<LicenseInfo> Licenses { get; }
}

public partial class LicenseService(ILogger<LicenseService> logger) : ILicenseService
{
    public IReadOnlyCollection<LicenseInfo> Licenses { get; } = LoadLicenses(logger);

    private static ReadOnlyCollection<LicenseInfo> LoadLicenses(ILogger<LicenseService> logger)
    {
        logger.LogInformation("Loading licenses from embedded resources.");

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        var licenseResourcePath = $"{assembly.GetName().Name}{ApplicationConstants.LicensesResourcePath}";

        List<LicenseInfo> loadedLicenses = [.. resourceNames
            .Where(name => name.StartsWith(licenseResourcePath) && name.EndsWith(ApplicationConstants.LicenseFileExtension))
            .Select(name => (name, stream: assembly.GetManifestResourceStream(name)))
            .Where(x => x.stream is not null)
            .Select(x =>
            {
                using var reader = new StreamReader(x.stream!);
                return new LicenseInfo(
                    Path.GetFileNameWithoutExtension(x.name[licenseResourcePath.Length..]),
                    Linkify(reader.ReadToEnd())
                );
            })];

        logger.LogInformation("{Count} licenses loaded.", loadedLicenses.Count);

        return loadedLicenses.AsReadOnly();
    }

    private static MarkupString Linkify(string text)
    {
        if (String.IsNullOrEmpty(text))
        {
            return new MarkupString("");
        }

        var regex = UrlRegex();
        var sb = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in regex.Matches(text))
        {
            sb.Append(System.Net.WebUtility.HtmlEncode(text[lastIndex..match.Index]));
            sb.Append($"<a href=\"{match.Value}\" target=\"_blank\">{match.Value}</a>");
            lastIndex = match.Index + match.Length;
        }

        sb.Append(System.Net.WebUtility.HtmlEncode(text.Substring(lastIndex)));

        return new MarkupString(sb.ToString());
    }

    [GeneratedRegex(@"(https?://[^\s,()<>]+)")]
    private static partial Regex UrlRegex();
}