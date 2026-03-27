using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Ui.Client.Interfaces;
using Askyl.Dsm.WebHosting.Ui.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add JSON configuration file (must be in wwwroot/)
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Configure Serilog using configuration from appsettings.json
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Logging.AddSerilog(dispose: true);

builder.Services.AddHttpClient(ApplicationConstants.HttpClientName, client =>
{
    // API controllers are hosted at the domain root (without /adwh path base)
    // Reverse proxy handles /adwh/api/... -> /api/... routing in production
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});
builder.Services.AddFluentUIComponents();

// Register authentication service as Singleton for app lifetime
// Authentication state is managed server-side via session cookies, not client storage
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

// Register services that call REST API endpoints
builder.Services.AddScoped<IDotnetVersionService, DotnetVersionService>();
builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();
builder.Services.AddScoped<IWebSiteHostingService, WebSiteHostingService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<IFileSystemService, FileSystemService>();

// Register tree content service for directory expansion
builder.Services.AddScoped<ITreeContentService, TreeContentService>();

var host = builder.Build();

await host.RunAsync();
