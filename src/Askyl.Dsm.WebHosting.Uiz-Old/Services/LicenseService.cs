using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Ui.Models;
using Askyl.Dsm.WebHosting.Constants;

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
                var content = reader.ReadToEnd();
                
                // Secure content validation
                if (!ValidateLicenseContent(content))
                {
                    logger.LogWarning("Invalid license content detected and skipped: {FileName}", Path.GetFileName(x.name));
                    return null!;
                }
                
                return new LicenseInfo(
                    Path.GetFileNameWithoutExtension(x.name[licenseResourcePath.Length..]),
                    Linkify(content)
                );
            })
            .Where(x => x != null)]; // Filter invalid contents

        logger.LogInformation("{Count} licenses loaded.", loadedLicenses.Count);

        return loadedLicenses.AsReadOnly();
    }

    private static bool ValidateLicenseContent(string content)
    {
        // Maximum size check (e.g. 100KB)
        if (content.Length > LicenseConstants.MaxLicenseSizeBytes)
        {
            return false;
        }

        // Check for malicious special characters
        // Prevent script injections
        if (content.Contains("<script") || content.Contains("javascript:"))
        {
            return false;
        }

        // Check for potentially dangerous URLs
        if (content.Contains("data:text/html") || content.Contains("eval("))
        {
            return false;
        }

        return true;
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
            // URL validation before link creation
            if (IsValidUrl(match.Value))
            {
                sb.Append(System.Net.WebUtility.HtmlEncode(text[lastIndex..match.Index]));
                sb.Append($"<a href=\"{match.Value}\" target=\"_blank\" rel=\"noopener noreferrer\">{match.Value}</a>");
                lastIndex = match.Index + match.Length;
            }
            else
            {
                // If URL is invalid, simply encode the text
                sb.Append(System.Net.WebUtility.HtmlEncode(text[lastIndex..match.Index]));
                sb.Append(System.Net.WebUtility.HtmlEncode(match.Value));
                lastIndex = match.Index + match.Length;
            }
        }

        sb.Append(System.Net.WebUtility.HtmlEncode(text.Substring(lastIndex)));

        return new MarkupString(sb.ToString());
    }

    private static bool IsValidUrl(string url)
    {
        if (String.IsNullOrEmpty(url))
            return false;
            
        try
        {
            var uri = new Uri(url);
            // Allow only http and https
            return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) 
                   && !uri.Host.Contains(".."); // Prevent malicious hosts
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex(@"(https?://[^\s,()<>]+)")]
    private static partial Regex UrlRegex();
}