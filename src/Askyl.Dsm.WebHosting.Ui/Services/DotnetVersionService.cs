using System.Diagnostics;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IDotnetVersionService
{
    Task<List<DotnetInstallation>> GetInstalledVersionsAsync();
    Task<bool> IsVersionInstalledAsync(string version, string frameworkType = "ASP.NET Core");
}

public class DotnetVersionService : IDotnetVersionService
{
    public async Task<List<DotnetInstallation>> GetInstalledVersionsAsync()
    {
        var installations = new List<DotnetInstallation>();

        try
        {
            // Use only the global dotnet version
            var installation = await GetDotnetInstallationAsync("dotnet");
            if (installation != null)
            {
                installations.Add(installation);
            }
        }
        catch
        {
            // Ignore errors - no .NET installation found
        }

        return installations;
    }

    public async Task<bool> IsVersionInstalledAsync(string version, string frameworkType = "ASP.NET Core")
    {
        try
        {
            var installations = await GetInstalledVersionsAsync();
            return installations.Any(install => 
                install.Frameworks.Any(framework => 
                    framework.Type == frameworkType && framework.Version == version));
        }
        catch
        {
            return false;
        }
    }

    private async Task<DotnetInstallation?> GetDotnetInstallationAsync(string dotnetPath)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = dotnetPath;
            process.StartInfo.Arguments = "--info";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !String.IsNullOrEmpty(output))
            {
                var frameworks = ParseDotnetInfo(output);
                if (frameworks.Any())
                {
                    return new DotnetInstallation
                    {
                        Path = dotnetPath,
                        Frameworks = frameworks
                    };
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    private List<FrameworkInfo> ParseDotnetInfo(string output)
    {
        var frameworks = new List<FrameworkInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        string? currentSection = null;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Detect main sections
            if (trimmedLine.StartsWith(".NET SDKs installed:"))
            {
                currentSection = "SDK";
                continue;
            }
            else if (trimmedLine.StartsWith(".NET runtimes installed:"))
            {
                currentSection = "Runtime";
                continue;
            }
            else if (trimmedLine.StartsWith(".NET SDK:"))
            {
                // Main SDK base section
                currentSection = "Main SDK";
                continue;
            }

            // Skip empty lines
            if (String.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Parse versions in each section
            if (currentSection == "SDK")
            {
                // Format: "  9.0.300 [/usr/local/share/dotnet/sdk]"
                var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"^\s*(\d+\.\d+\.\d+)");
                if (match.Success)
                {
                    var version = match.Groups[1].Value;
                    if (!frameworks.Any(f => f.Type == "SDK" && f.Version == version))
                    {
                        frameworks.Add(new FrameworkInfo
                        {
                            Type = "SDK",
                            Version = version
                        });
                    }
                }
            }
            else if (currentSection == "Runtime")
            {
                // Format: "  Microsoft.AspNetCore.App 9.0.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]"
                if (trimmedLine.Contains("Microsoft.AspNetCore.App"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"Microsoft\.AspNetCore\.App\s+(\d+\.\d+\.\d+)");
                    if (match.Success)
                    {
                        var version = match.Groups[1].Value;
                        if (!frameworks.Any(f => f.Type == "ASP.NET Core" && f.Version == version))
                        {
                            frameworks.Add(new FrameworkInfo
                            {
                                Type = "ASP.NET Core",
                                Version = version
                            });
                        }
                    }
                }
                else if (trimmedLine.Contains("Microsoft.NETCore.App"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"Microsoft\.NETCore\.App\s+(\d+\.\d+\.\d+)");
                    if (match.Success)
                    {
                        var version = match.Groups[1].Value;
                        if (!frameworks.Any(f => f.Type == "Runtime" && f.Version == version))
                        {
                            frameworks.Add(new FrameworkInfo
                            {
                                Type = "Runtime",
                                Version = version
                            });
                        }
                    }
                }
            }
            else if (currentSection == "Main SDK")
            {
                // Format: " Version:           9.0.301"
                if (trimmedLine.StartsWith("Version:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"Version:\s*(\d+\.\d+\.\d+)");
                    if (match.Success)
                    {
                        var version = match.Groups[1].Value;
                        if (!frameworks.Any(f => f.Type == "SDK (Main)" && f.Version == version))
                        {
                            frameworks.Add(new FrameworkInfo
                            {
                                Type = "SDK (Main)",
                                Version = version
                            });
                        }
                    }
                }
            }
        }

        return frameworks.OrderBy(f => GetFrameworkOrder(f.Type)).ThenBy(f => f.Version).ToList();
    }

    private int GetFrameworkOrder(string frameworkType)
    {
        return frameworkType switch
        {
            "SDK (Main)" => 1,
            "SDK" => 2,
            "Runtime" => 3,
            "ASP.NET Core" => 4,
            _ => 5
        };
    }
}

public class DotnetInstallation
{
    public string Path { get; set; } = "";
    public List<FrameworkInfo> Frameworks { get; set; } = new();
}

public class FrameworkInfo
{
    public string Type { get; set; } = "";
    public string Version { get; set; } = "";
}
