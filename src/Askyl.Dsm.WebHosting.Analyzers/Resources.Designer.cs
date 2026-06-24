#nullable enable

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace Askyl.Dsm.WebHosting.Analyzers;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DesignerCategory("code")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
internal static class Resources
{
    private static ResourceManager? _resourceManager;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
        get => _resourceManager ??= new ResourceManager(typeof(Resources));
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo? Culture
    {
        get => Thread.CurrentThread.CurrentUICulture;
        set
        {
            if (value is not null)
                Thread.CurrentThread.CurrentUICulture = value;
        }
    }

    internal static string ADWH01001_Title => ResourceManager.GetString(nameof(ADWH01001_Title), Culture)!;
    internal static string ADWH01001_Message => ResourceManager.GetString(nameof(ADWH01001_Message), Culture)!;
    internal static string ADWH01001_Description => ResourceManager.GetString(nameof(ADWH01001_Description), Culture)!;
    internal static string ADWH01002_Title => ResourceManager.GetString(nameof(ADWH01002_Title), Culture)!;
    internal static string ADWH01002_Message => ResourceManager.GetString(nameof(ADWH01002_Message), Culture)!;
    internal static string ADWH01002_Description => ResourceManager.GetString(nameof(ADWH01002_Description), Culture)!;
    internal static string ADWH02001_Title => ResourceManager.GetString(nameof(ADWH02001_Title), Culture)!;
    internal static string ADWH02001_Message => ResourceManager.GetString(nameof(ADWH02001_Message), Culture)!;
    internal static string ADWH02001_Description => ResourceManager.GetString(nameof(ADWH02001_Description), Culture)!;
    internal static string ADWH03001_Title => ResourceManager.GetString(nameof(ADWH03001_Title), Culture)!;
    internal static string ADWH03001_Message => ResourceManager.GetString(nameof(ADWH03001_Message), Culture)!;
    internal static string ADWH03001_Description => ResourceManager.GetString(nameof(ADWH03001_Description), Culture)!;
}
