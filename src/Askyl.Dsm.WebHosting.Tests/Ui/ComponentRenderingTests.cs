using System.Globalization;
using System.Net;
using Askyl.Dsm.WebHosting.Constants.Globalization;
using Askyl.Dsm.WebHosting.Constants.UI;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Extensions;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using Askyl.Dsm.WebHosting.Ui.Client.Components.Controls;
using Askyl.Dsm.WebHosting.Ui.Client.Components.Dialogs;
using Askyl.Dsm.WebHosting.Ui.Client.Components.Pages;
using Askyl.Dsm.WebHosting.Ui.Client.Extensions;
using Askyl.Dsm.WebHosting.Ui.Client.Interfaces;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Bunit;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui;

/// <summary>
/// Renders the pages and dialogs with the real FluentUI services, to catch what only shows at runtime.
/// </summary>
/// <remarks>
/// The FluentUI 5 compile spike built clean and passed every test while its login page threw on load, its
/// number fields fetched a script from a CDN and its error dialogs threw on an angle bracket. None of that
/// was visible without rendering; these tests render.
/// JavaScript interop runs in loose mode, so it returns defaults, but every call is recorded.
/// </remarks>
public class ComponentRenderingTests : IAsyncLifetime
{
    /// <summary>
    /// The message System.Text.Json raises for an HTML body, measured; it reaches error dialogs through
    /// <c>ex.Message</c> whenever a 2xx response carries a page instead of JSON.
    /// </summary>
    const string HtmlBodyJsonError = "'<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.";

    /// <summary>
    /// The JavaScript namespace whose number and pattern masks load IMask from unpkg.com.
    /// </summary>
    const string CdnBackedMaskInterop = "TextMasked";

    /// <summary>
    /// Where FluentMessageBox renders its text (FluentMessageBox.razor at v5.0.1).
    /// </summary>
    const string MessageBoxTextSelector = ".fluent-message-box .message";

    const string SiteName = "alpha-site";

    /// <summary>
    /// FluentDataGrid's header cells, whichever of its display modes rendered them.
    /// </summary>
    const string ColumnHeaderSelector = "th, [role=columnheader]";

    const string EnglishCulture = "en-US";

    /// <summary>
    /// FluentUI's key for the title of an error message box.
    /// </summary>
    const string MessageBoxErrorKey = "MessageBox_Error";

    const string MessageBoxYesKey = "MessageBox_ButtonYes";

    const string MessageBoxNoKey = "MessageBox_ButtonNo";

    const string FrenchCulture = "fr-FR";

    readonly BunitContext _ctx = new();
    readonly Mock<IAuthenticationService> _authService = new();
    readonly Mock<IWebSiteHostingService> _hostingService = new();
    readonly Mock<ILicenseService> _licenseService = new();
    readonly Mock<IDsmConnectionDiagnosticService> _diagnosticService = new();
    readonly ILocalizer _localizer;

    public ComponentRenderingTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddFluentUIComponentsWithDefaults();
        _ctx.Services.AddGlobalization();
        _ctx.Services.AddValidatorsFromAssemblyContaining<SharedResource>();
        _ctx.Services.AddSingleton(_authService.Object);
        _ctx.Services.AddSingleton(_hostingService.Object);
        _ctx.Services.AddSingleton(_licenseService.Object);
        _ctx.Services.AddSingleton(_diagnosticService.Object);
        _ctx.Services.AddSingleton(Mock.Of<ICultureManager>());

        _authService.Setup(service => service.IsAuthenticatedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ApiResultBool.CreateSuccess(false));
        _diagnosticService.Setup(service => service.GetReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Report(isHealthy: true));
        _hostingService.Setup(service => service.GetAllWebsitesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebSiteInstancesResult.CreateSuccess([new() { Configuration = new() { Name = SiteName } }]));

        _localizer = _ctx.Services.GetRequiredService<ILocalizer>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    // FluentUI registers services that only implement IAsyncDisposable
    public async Task DisposeAsync() => await _ctx.DisposeAsync();

    [Fact]
    public void Stacks_KeepTheGapFluentUi4AppliedByDefault()
    {
        var cut = _ctx.Render<NotFound>();

        // NotFound sets no gap of its own, so every gap here comes from the registered defaults
        var gaps = cut.FindAll("[style*='column-gap']");

        Assert.Equal(2, gaps.Count);
        Assert.All(gaps, stack => Assert.Contains($"column-gap: {LayoutConstants.StackGap}", stack.GetAttribute("style"), StringComparison.Ordinal));
    }

    [Fact]
    public void Login_RendersItsThreeCredentialFields()
    {
        var cut = _ctx.Render<Login>();

        Assert.Single(cut.FindAll("[name=username]"));
        Assert.Single(cut.FindAll("[name=password]"));
        Assert.Single(cut.FindAll("[name=otp]"));
    }

    [Fact]
    public void Login_ShowsTheDiagnostic_InsteadOfTheForm_WhenDsmIsUnreachable()
    {
        _diagnosticService.Setup(service => service.GetReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Report(isHealthy: false));

        var cut = _ctx.Render<Login>();

        cut.WaitForAssertion(() => Assert.Contains(_localizer[LK.Diagnostic.FailureHostnameMissing], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal));
        Assert.Empty(cut.FindAll("[name=username]"));
    }

    [Fact]
    public void DsmDiagnosticPanel_ReCheck_RaisesTheCallback()
    {
        var rechecked = false;

        var cut = _ctx.Render<DsmDiagnosticPanel>(parameters => parameters
            .Add(panel => panel.Report, Report(isHealthy: false).Value!)
            .Add(panel => panel.OnRecheck, () => rechecked = true));

        // The message bar's dismiss button is a fluent-button too; the re-check one is the primary
        cut.Find("fluent-button[appearance=primary]").Click();

        Assert.True(rechecked);
    }

    [Fact]
    public void DsmDiagnosticPanel_OmitsTheCertificateLines_ThatHaveNoValue()
    {
        var report = new DsmConnectionReport(false, DateTimeOffset.UtcNow, "nas.example", 5001,
        [
            new(DsmConnectionCheck.CertificateCoversName, DsmCheckStatus.Failed, DsmCheckFailure.CertificateUntrusted, new DsmCertificateSummary([], String.Empty, DateTimeOffset.UtcNow))
        ]);

        var cut = _ctx.Render<DsmDiagnosticPanel>(parameters => parameters.Add(panel => panel.Report, report));

        // Decoded, since the markup writes > as &gt;
        var text = WebUtility.HtmlDecode(cut.Markup);

        Assert.Contains(_localizer[LK.Diagnostic.FailureCertificateUntrusted], text, StringComparison.Ordinal);
        Assert.DoesNotContain(_localizer[LK.Diagnostic.CertificateNames, String.Empty], text, StringComparison.Ordinal);
        Assert.DoesNotContain(_localizer[LK.Diagnostic.CertificateIssuer, String.Empty], text, StringComparison.Ordinal);
    }

    [Fact]
    public void DsmDiagnosticPanel_FailureBar_CannotBeDismissed()
    {
        // The bar holds the only remedy on the page; FluentMessageBar offers a dismiss button by default
        var cut = _ctx.Render<DsmDiagnosticPanel>(parameters => parameters.Add(panel => panel.Report, Report(isHealthy: false).Value!));

        Assert.Single(cut.FindAll("fluent-message-bar"));
        Assert.Empty(cut.FindAll("fluent-message-bar fluent-button"));
        Assert.Single(cut.FindAll("fluent-button[appearance=primary]"));
    }

    static DsmConnectionReportResult Report(bool isHealthy)
        => DsmConnectionReportResult.CreateSuccess(isHealthy
            ? new DsmConnectionReport(true, DateTimeOffset.UtcNow, null, 0, [])
            : new DsmConnectionReport(false, DateTimeOffset.UtcNow, null, 5001,
            [
                new(DsmConnectionCheck.HostnameConfigured, DsmCheckStatus.Failed, DsmCheckFailure.HostnameMissing),
                .. Enum.GetValues<DsmConnectionCheck>().Skip(1).Select(check => new DsmConnectionCheckResult(check, DsmCheckStatus.Skipped))
            ]));

    [Fact]
    public void Home_ListsTheWebsitesAndNamesItsIconOnlyButtons()
    {
        var cut = _ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains(SiteName, cut.Markup, StringComparison.Ordinal));

        var names = cut.FindAll("[aria-label]").Select(element => element.GetAttribute("aria-label")).ToHashSet();

        Assert.Contains(_localizer[LK.Home.StartButton], names);
        Assert.Contains(_localizer[LK.Home.StopButton], names);
    }

    [Fact]
    public void LoadingOverlay_IsFullScreen()
    {
        // Otherwise FluentUI 5 positions the loader from its parent's box, which inside a dialog is 0x0 at the
        // bottom-left of the viewport: the loader showed in a corner of the screen
        var cut = _ctx.Render<Home>();

        Assert.True(cut.Find("fluent-overlay").HasAttribute("fullscreen"));
    }

    [Fact]
    public void LoadingOverlay_TextTakesTheThemeForeground()
    {
        // The overlay's native dialog sets the browser's own text colour, black on the dark theme, unless the text sets one
        var cut = _ctx.Render<Home>();

        Assert.Contains("var(--colorNeutralForeground1)", cut.Find("fluent-overlay fluent-text").GetAttribute("style"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LicenseTabs_GiveFluentUITheirLabel()
    {
        // fluent-tab reads its label before Blazor inserts it, so its bold-width reserve stayed empty and selecting the
        // widest license name shifted the license text sideways
        _licenseService.Setup(service => service.GetLicensesAsync()).ReturnsAsync([new(SiteName, SiteName)]);

        var provider = _ctx.Render<FluentDialogProvider>();
        var dialogService = _ctx.Services.GetRequiredService<IDialogService>();

        await provider.InvokeAsync(() => { _ = dialogService.ShowDialogAsync<LicensesDialog>(options => { }); });

        provider.WaitForAssertion(() => Assert.Contains($"{LayoutConstants.TabLabelProperty}: '{SiteName}'", provider.Find("fluent-tab").GetAttribute("style"), StringComparison.Ordinal));
    }

    [Fact]
    public void HomeGrid_KeepsItsColumnOrderWhenTheCultureChanges()
    {
        // FluentUI 5 remembers column order by key, and a column without ColumnId is keyed by its title: once the
        // titles were translated, only "Framework" still matched its old key and jumped to the front. Seen in a
        // browser after the login culture (en-US) gave way to the system one (fr-FR).
        string[] keys = [LK.Home.GridColumnName, LK.Home.GridColumnPath, LK.Home.GridColumnFramework, LK.Home.GridColumnInternalPort, LK.Home.GridColumnState];
        var original = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(EnglishCulture);
            var cut = _ctx.Render<Home>();
            cut.WaitForAssertion(() => Assert.Contains(SiteName, cut.Markup, StringComparison.Ordinal));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(FrenchCulture);
            cut.Render();

            string[] expected = [.. keys.Select(key => _localizer[key])];
            string[] headers = [.. cut.FindAll(ColumnHeaderSelector).Select(header => header.TextContent.Trim())];

            Assert.Equal(expected, headers);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public async Task ErrorDialog_DisplaysAMessageContainingAnAngleBracket()
    {
        var cut = _ctx.Render<FluentDialogProvider>();
        var dialogService = _ctx.Services.GetRequiredService<IDialogService>();

        // A dialog's task completes when it closes: a statement lambda keeps InvokeAsync from awaiting it
        Task<DialogResult>? shown = null;
        await cut.InvokeAsync(() => { shown = dialogService.ShowErrorTextAsync(HtmlBodyJsonError); });

        // Text content, not markup: an encoding rendered as text would show "&lt;" instead of the bracket
        Assert.False(shown!.IsFaulted, shown.Exception?.ToString());
        cut.WaitForAssertion(() => Assert.Contains(HtmlBodyJsonError, cut.Find(MessageBoxTextSelector).TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Confirmation_ButtonsFollowTheUICulture()
    {
        // FluentUI's default buttons are an English "Yes" and "No"; FluentUILocalizer translates them
        var original = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(FrenchCulture);
            string?[] expected = [.. new[] { MessageBoxYesKey, MessageBoxNoKey }.Select(key => Localizer.SharedResource.GetString(GlobalizationConstants.FluentUIResourcePrefix + key, CultureInfo.CurrentUICulture))];

            var cut = _ctx.Render<FluentDialogProvider>();
            var dialogService = _ctx.Services.GetRequiredService<IDialogService>();

            Task<DialogResult>? shown = null;
            await cut.InvokeAsync(() => { shown = dialogService.ShowConfirmationTextAsync(SiteName); });

            Assert.False(shown!.IsFaulted, shown.Exception?.ToString());
            Assert.All(expected, Assert.NotNull);
            cut.WaitForAssertion(() => Assert.Equal(expected, cut.FindAll("fluent-button").Select(button => button.TextContent.Trim()).Where(text => text.Length > 0)));
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public async Task EveryFluentUIStringTheScreensAskFor_HasAFrenchTranslation()
    {
        // FluentUI 5 ships English only: a key it asks for that the resources lack shows English in a French UI
        var recorder = new RecordingFluentLocalizer();
        _ctx.Services.GetRequiredService<LibraryConfiguration>().Localizer = recorder;

        _ctx.Render<Login>();
        _ctx.Render<NotFound>();
        var home = _ctx.Render<Home>();
        home.WaitForAssertion(() => Assert.Contains(SiteName, home.Markup, StringComparison.Ordinal));

        var provider = _ctx.Render<FluentDialogProvider>();
        var dialogService = _ctx.Services.GetRequiredService<IDialogService>();

        await provider.InvokeAsync(() =>
        {
            _ = dialogService.ShowErrorTextAsync(SiteName);
            _ = dialogService.ShowInfoTextAsync(SiteName);
            _ = dialogService.ShowConfirmationTextAsync(SiteName);
            _ = dialogService.ShowDialogAsync<WebSiteConfigurationDialog>(options => options.Parameters[nameof(WebSiteConfigurationDialog.Content)] = new WebSiteInstance());
        });

        provider.WaitForAssertion(() => Assert.Contains(_localizer[LK.WebsiteConfig.InternalPortLabel], provider.Markup, StringComparison.Ordinal));

        var french = Localizer.SharedResource.GetResourceSet(CultureInfo.GetCultureInfo(FrenchCulture), createIfNotExists: true, tryParents: false)!;
        string[] untranslated = [.. recorder.Keys.Where(key => french.GetString(GlobalizationConstants.FluentUIResourcePrefix + key) is null).Order(StringComparer.Ordinal)];

        // Guard: a recorder that was never called would pass in green
        Assert.NotEmpty(recorder.Keys);
        Assert.True(untranslated.Length == 0, String.Join(Environment.NewLine, untranslated));
    }

    [Fact]
    public async Task ErrorDialog_TitleFollowsTheUICulture()
    {
        var original = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(FrenchCulture);
            var expected = Localizer.SharedResource.GetString(GlobalizationConstants.FluentUIResourcePrefix + MessageBoxErrorKey, CultureInfo.CurrentUICulture);

            var cut = _ctx.Render<FluentDialogProvider>();
            var dialogService = _ctx.Services.GetRequiredService<IDialogService>();
            await cut.InvokeAsync(() => { _ = dialogService.ShowErrorTextAsync(SiteName); });

            Assert.NotNull(expected);
            cut.WaitForAssertion(() => Assert.Contains(expected, cut.Markup, StringComparison.Ordinal));
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    /// <summary>
    /// Records every FluentUI string key a render asks for, and answers with the application's localizer.
    /// </summary>
    sealed class RecordingFluentLocalizer : IFluentLocalizer
    {
        readonly FluentUILocalizer _inner = new();
        readonly Lock _lock = new();
        readonly HashSet<string> _keys = [];

        public IReadOnlyCollection<string> Keys
        {
            get
            {
                lock (_lock)
                {
                    return [.. _keys];
                }
            }
        }

        public string this[string key, params object[] arguments]
        {
            get
            {
                lock (_lock)
                {
                    _keys.Add(key);
                }

                return _inner[key, arguments];
            }
        }
    }

    [Fact]
    public async Task WebsiteDialog_OpensWithoutLoadingAMaskScript()
    {
        var cut = _ctx.Render<FluentDialogProvider>();
        var dialogService = _ctx.Services.GetRequiredService<IDialogService>();

        Task<DialogResult>? shown = null;
        await cut.InvokeAsync(() => { shown = dialogService.ShowDialogAsync<WebSiteConfigurationDialog>(options => options.Parameters[nameof(WebSiteConfigurationDialog.Content)] = new WebSiteInstance()); });

        Assert.False(shown!.IsFaulted, shown.Exception?.ToString());
        cut.WaitForAssertion(() => Assert.Contains(_localizer[LK.WebsiteConfig.InternalPortLabel], cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain(_ctx.JSInterop.Invocations, invocation => invocation.Identifier.Contains(CdnBackedMaskInterop, StringComparison.Ordinal));
    }
}
