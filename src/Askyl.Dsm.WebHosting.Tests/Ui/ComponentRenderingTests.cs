using System.Globalization;
using System.Net;
using Askyl.Dsm.WebHosting.Constants.Application;
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
using Askyl.Dsm.WebHosting.Ui.Client.Components.Patterns.WorkingState;
using Askyl.Dsm.WebHosting.Ui.Client.Extensions;
using Askyl.Dsm.WebHosting.Ui.Client.Interfaces;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Bunit;
using FluentValidation;
using Microsoft.AspNetCore.Components;
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

    const int DsmPort = 5001;

    readonly BunitContext _ctx = new();
    readonly Mock<IAuthenticationService> _authService = new();
    readonly Mock<IWebSiteHostingService> _hostingService = new();
    readonly Mock<ILicenseService> _licenseService = new();
    readonly Mock<IDsmConnectionDiagnosticService> _diagnosticService = new();
    readonly ManualTimeProvider _time = new();
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
        _ctx.Services.AddSingleton(new DsmHttpsEndpoint(5001));
        _ctx.Services.AddSingleton<TimeProvider>(_time);

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
    public void Login_OverPlainHttp_OffersTheHttpsVersion_InsteadOfTheForm()
    {
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();

        navigation.NavigateTo("http://nas.test:5000/adwh/login");

        var cut = _ctx.Render<Login>();

        Assert.Contains(_localizer[LK.Login.InsecureTitle], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[name=username]"));
        _authService.Verify(service => service.IsAuthenticatedAsync(It.IsAny<CancellationToken>()), Times.Never);
        _diagnosticService.Verify(service => service.GetReportAsync(It.IsAny<CancellationToken>()), Times.Never);

        cut.Find("fluent-button[appearance=primary]").Click();

        Assert.Equal("https://nas.test:5001/adwh/login", navigation.Uri);
    }

    [Fact]
    public void Login_ShowsTheDiagnostic_InsteadOfTheForm_WhenDsmIsUnreachable()
    {
        _diagnosticService.Setup(service => service.GetReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Report(isHealthy: false));

        var cut = _ctx.Render<Login>();

        cut.WaitForAssertion(() => Assert.Contains(_localizer[LK.Diagnostic.FailurePortRefused, DsmPort], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal));
        Assert.Empty(cut.FindAll("[name=username]"));
    }

    [Fact]
    public void Login_SwitchesToTheDiagnostic_WhenTheLoginFindsDsmUnreachable()
    {
        _diagnosticService.SetupSequence(service => service.GetReportAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Report(isHealthy: true))
            .ReturnsAsync(Report(isHealthy: false));
        _authService.Setup(service => service.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.CreateDsmUnreachable("unreachable"));

        var cut = _ctx.Render<Login>();

        cut.Find("[name=username]").Input("admin");
        cut.Find("[name=password]").Input("password123");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains(_localizer[LK.Diagnostic.FailurePortRefused, DsmPort], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal));
        Assert.Empty(cut.FindAll("[name=username]"));
        _authService.Verify(service => service.LoginAsync("admin", "password123", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Login_WhenTheServerCannotBeReached_StillShowsTheForm_AndSaysWhy()
    {
        var toasts = CaptureToasts();
        _authService.Setup(service => service.IsAuthenticatedAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());

        var cut = _ctx.Render<Login>();

        cut.WaitForAssertion(() => VerifyToldTheServerDidNotAnswer(toasts));
        Assert.False(cut.Instance.IsWorking);
        Assert.Single(cut.FindAll("[name=username]"));
    }

    [Fact]
    public void Login_WhenTheServerNeverAnswersTheSignIn_SaysSo()
    {
        var toasts = CaptureToasts();
        _authService.Setup(service => service.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(HttpClientTimeout());

        var cut = _ctx.Render<Login>();

        cut.Find("[name=username]").Input("admin");
        cut.Find("[name=password]").Input("password123");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => VerifyToldTheServerDidNotAnswer(toasts));
        Assert.False(cut.Instance.IsWorking);
        Assert.Single(cut.FindAll("[name=username]"));
    }

    [Fact]
    public void Login_WhenTheReCheckNeverAnswers_KeepsTheDiagnostic_AndSaysSo()
    {
        var toasts = CaptureToasts();
        _diagnosticService.Setup(service => service.GetReportAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Report(isHealthy: false));
        _diagnosticService.Setup(service => service.RecheckAsync(It.IsAny<CancellationToken>())).ThrowsAsync(HttpClientTimeout());

        var cut = _ctx.Render<Login>();

        // The panel's re-check is the page's only primary button
        cut.WaitForAssertion(() => cut.Find("fluent-button[appearance=primary]").Click());

        cut.WaitForAssertion(() => VerifyToldTheServerDidNotAnswer(toasts));
        Assert.False(cut.Instance.IsWorking);
        Assert.Contains(_localizer[LK.Diagnostic.FailurePortRefused, DsmPort], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal);
    }

    /// <summary>
    /// Renders where FluentUI shows its toasts, so a test can see what the user was told. Moq cannot stand in for
    /// INotificationService: it has a setter Castle cannot implement.
    /// </summary>
    IRenderedComponent<FluentToastProvider> CaptureToasts() => _ctx.Render<FluentToastProvider>();

    void VerifyToldTheServerDidNotAnswer(IRenderedComponent<FluentToastProvider> toasts)
        => Assert.Contains(_localizer[LK.Error.ServerNotResponding], WebUtility.HtmlDecode(toasts.Markup), StringComparison.Ordinal);

    // What HttpClient throws when its own Timeout runs out
    static TaskCanceledException HttpClientTimeout() => new(null, new TimeoutException());

    [Fact]
    public void DsmDiagnosticPanel_ListsTheThreeChecks()
    {
        var cut = _ctx.Render<DsmDiagnosticPanel>(parameters => parameters.Add(panel => panel.Report, Report(isHealthy: false).Value!));

        var text = WebUtility.HtmlDecode(cut.Markup);
        List<int> positions = [.. new[] { LK.Diagnostic.CheckPortAnswers, LK.Diagnostic.CheckConnectionTrusted, LK.Diagnostic.CheckApiAnswers }
            .Select(key => text.IndexOf(_localizer[key], StringComparison.Ordinal))];

        Assert.DoesNotContain(-1, positions);
        Assert.Equal(positions.Order(), positions);
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
    public void DsmDiagnosticPanel_AnUntrustedConnection_NamesThePort()
    {
        var report = new DsmConnectionReport(false, DateTimeOffset.UtcNow, "127.0.0.1", DsmPort,
        [
            new(DsmConnectionCheck.PortAnswers, DsmCheckStatus.Passed),
            new(DsmConnectionCheck.ConnectionTrusted, DsmCheckStatus.Failed, DsmCheckFailure.NotTrusted),
            new(DsmConnectionCheck.ApiAnswers, DsmCheckStatus.Skipped)
        ]);

        var cut = _ctx.Render<DsmDiagnosticPanel>(parameters => parameters.Add(panel => panel.Report, report));

        // Decoded, since the markup writes > as &gt;
        Assert.Contains(_localizer[LK.Diagnostic.FailureNotTrusted, DsmPort], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal);
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
            : new DsmConnectionReport(false, DateTimeOffset.UtcNow, "127.0.0.1", DsmPort,
            [
                new(DsmConnectionCheck.PortAnswers, DsmCheckStatus.Failed, DsmCheckFailure.PortRefused),
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
    public void Home_Logout_CoversThePageUntilTheServerAnswers()
    {
        var logout = new TaskCompletionSource<ApiResult>();
        _authService.Setup(service => service.LogoutAsync(It.IsAny<CancellationToken>())).Returns(logout.Task);

        var cut = _ctx.Render<Home>();
        cut.WaitForAssertion(() => Assert.Contains(SiteName, cut.Markup, StringComparison.Ordinal));

        cut.FindAll("fluent-button").Single(button => button.TextContent.Contains(_localizer[LK.Home.LogoutButton], StringComparison.Ordinal)).Click();

        Assert.True(cut.Instance.IsWorking);
        Assert.Contains(_localizer[LK.Loading.LoggingOut], WebUtility.HtmlDecode(cut.Find("fluent-overlay").TextContent), StringComparison.Ordinal);

        logout.SetResult(ApiResult.CreateSuccess());

        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsWorking));
    }

    [Fact]
    public void LoadingOverlay_CountsTheSecondsOutLoud_OnceTheWaitGetsLong()
    {
        const int threshold = ApplicationConstants.LoadingElapsedThresholdSeconds;
        var worker = new Worker { IsWorking = true, Message = SiteName };
        var cut = _ctx.Render<LoadingOverlay>(parameters => parameters.Add(overlay => overlay.WorkingState, worker));

        _time.Advance(TimeSpan.FromSeconds(threshold - 1));

        Assert.DoesNotContain(_localizer[LK.Loading.StillWorking, threshold - 1], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal);

        _time.Advance(TimeSpan.FromSeconds(8));

        cut.WaitForAssertion(() => Assert.Contains(_localizer[LK.Loading.StillWorking, threshold + 7], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal));
    }

    [Fact]
    public void LoadingOverlay_CountsEachWaitFromItsOwnStart()
    {
        var worker = new Worker { IsWorking = true, Message = SiteName };
        var cut = _ctx.Render<LoadingOverlay>(parameters => parameters.Add(overlay => overlay.WorkingState, worker));

        _time.Advance(TimeSpan.FromSeconds(30));
        worker.IsWorking = false;
        cut.Render();
        worker.IsWorking = true;
        cut.Render();
        _time.Advance(TimeSpan.FromSeconds(6));

        cut.WaitForAssertion(() => Assert.Contains(_localizer[LK.Loading.StillWorking, 6], WebUtility.HtmlDecode(cut.Markup), StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoadingOverlay_LeavesNoTimerBehind()
    {
        // Including when its page goes away mid-wait, as the login page does once signed in
        var worker = new Worker { IsWorking = true, Message = SiteName };
        var cut = _ctx.Render<LoadingOverlay>(parameters => parameters.Add(overlay => overlay.WorkingState, worker));

        // Guard: a clock that never saw the timer would pass both checks below
        Assert.Equal(1, _time.ActiveTimers);

        worker.IsWorking = false;
        cut.Render();

        Assert.Equal(0, _time.ActiveTimers);

        worker.IsWorking = true;
        cut.Render();
        await _ctx.DisposeComponentsAsync();

        Assert.Equal(0, _time.ActiveTimers);
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

    /// <summary>
    /// The state a page hands its loading overlay, without the page.
    /// </summary>
    sealed class Worker : WorkingStateBase;

    /// <summary>
    /// A clock that moves only when told to, and fires every timer it created each time it does.
    /// </summary>
    sealed class ManualTimeProvider : TimeProvider
    {
        readonly List<ManualTimer> _timers = [];
        long _timestamp;

        public int ActiveTimers => _timers.Count;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(() => callback(state), stopped => _timers.Remove(stopped));
            _timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan duration)
        {
            _timestamp += duration.Ticks;

            foreach (var timer in _timers.ToArray())
            {
                timer.Fire();
            }
        }

        sealed class ManualTimer(Action fire, Action<ManualTimer> stop) : ITimer
        {
            public void Fire() => fire();

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose() => stop(this);

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
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
