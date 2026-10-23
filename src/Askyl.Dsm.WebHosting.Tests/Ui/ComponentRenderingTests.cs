using System.Globalization;
using Askyl.Dsm.WebHosting.Constants.UI;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Extensions;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using Askyl.Dsm.WebHosting.Ui.Client.Components.Dialogs;
using Askyl.Dsm.WebHosting.Ui.Client.Components.Pages;
using Askyl.Dsm.WebHosting.Ui.Client.Extensions;
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
/// number fields fetched a script from a CDN and its error dialogs threw on an angle bracket
/// (docs/ai/fluentui-5-migration.md §4). None of that was visible without rendering; these tests render.
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

    const string FrenchCulture = "fr-FR";

    readonly BunitContext _ctx = new();
    readonly Mock<IAuthenticationService> _authService = new();
    readonly Mock<IWebSiteHostingService> _hostingService = new();
    readonly ILocalizer _localizer;

    public ComponentRenderingTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddFluentUIComponentsWithDefaults();
        _ctx.Services.AddGlobalization();
        _ctx.Services.AddValidatorsFromAssemblyContaining<SharedResource>();
        _ctx.Services.AddSingleton(_authService.Object);
        _ctx.Services.AddSingleton(_hostingService.Object);
        _ctx.Services.AddSingleton(Mock.Of<ICultureManager>());

        _authService.Setup(service => service.IsAuthenticatedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ApiResultBool.CreateSuccess(false));
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
    public void Home_ListsTheWebsitesAndNamesItsIconOnlyButtons()
    {
        var cut = _ctx.Render<Home>();

        cut.WaitForAssertion(() => Assert.Contains(SiteName, cut.Markup, StringComparison.Ordinal));

        var names = cut.FindAll("[aria-label]").Select(element => element.GetAttribute("aria-label")).ToHashSet();

        Assert.Contains(_localizer[LK.Home.StartButton], names);
        Assert.Contains(_localizer[LK.Home.StopButton], names);
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
