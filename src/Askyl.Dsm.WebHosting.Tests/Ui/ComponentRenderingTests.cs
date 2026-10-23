using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Extensions;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using Askyl.Dsm.WebHosting.Ui.Client.Components.Dialogs;
using Askyl.Dsm.WebHosting.Ui.Client.Components.Pages;
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

    const string SiteName = "alpha-site";

    readonly BunitContext _ctx = new();
    readonly Mock<IAuthenticationService> _authService = new();
    readonly Mock<IWebSiteHostingService> _hostingService = new();
    readonly ILocalizer _localizer;

    public ComponentRenderingTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddFluentUIComponents();
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
    public async Task ErrorDialog_DisplaysAMessageContainingAnAngleBracket()
    {
        var cut = _ctx.Render<FluentDialogProvider>();
        var dialogService = _ctx.Services.GetRequiredService<IDialogService>();

        await cut.InvokeAsync(() => dialogService.ShowErrorAsync(HtmlBodyJsonError));

        cut.WaitForAssertion(() => Assert.Contains("is an invalid start of a value", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task WebsiteDialog_OpensWithoutLoadingAMaskScript()
    {
        var cut = _ctx.Render<FluentDialogProvider>();
        var dialogService = _ctx.Services.GetRequiredService<IDialogService>();

        await cut.InvokeAsync(() => dialogService.ShowDialogAsync<WebSiteConfigurationDialog>(new WebSiteInstance(), []));

        cut.WaitForAssertion(() => Assert.Contains(_localizer[LK.WebsiteConfig.InternalPortLabel], cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain(_ctx.JSInterop.Invocations, invocation => invocation.Identifier.Contains(CdnBackedMaskInterop, StringComparison.Ordinal));
    }
}
