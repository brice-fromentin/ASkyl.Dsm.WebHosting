using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.WebSites;
using Askyl.Dsm.WebHosting.Tools;
using Askyl.Dsm.WebHosting.Tools.WebSites;
using Askyl.Dsm.WebHosting.Ui.Components;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddDsmApiClient();
builder.Services.AddFluentUIComponents();

// Add custom services
builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();
builder.Services.AddScoped<IDotnetVersionService, DotnetVersionService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<ILogDownloadService, LogDownloadService>();
builder.Services.AddScoped<ITemporaryTokenService, TemporaryTokenService>();

// Add WebSite management services (late configuration)
builder.Services.AddSingleton<IReverseProxyManager, ReverseProxyManager>();
builder.Services.AddSingleton<IWebSitesConfigurationService, WebSitesConfigurationService>();
builder.Services.AddSingleton<WebSiteHostingService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<WebSiteHostingService>());

// Add services to the container.
builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

builder.Services.AddControllers();

builder.Services.AddMemoryCache();

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

var app = builder.Build();

app.UsePathBase(ApplicationConstants.ApplicationUrlSubPath);
app.MapBlazorHub(ApplicationConstants.ApplicationUrlSubPath + "/_blazor");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();
