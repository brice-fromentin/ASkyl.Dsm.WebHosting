using System.Net;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Globalization.Extensions;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Tools.Runtime;
using Askyl.Dsm.WebHosting.Ui.Components;
using Askyl.Dsm.WebHosting.Ui.Endpoints;
using Askyl.Dsm.WebHosting.Ui.Extensions;
using Askyl.Dsm.WebHosting.Ui.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Middleware;
using Askyl.Dsm.WebHosting.Ui.Services;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog();

// Add session services for authentication persistence
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "ADWH.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromMinutes(ApplicationConstants.SessionTimeoutMinutes);
});

builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();

// Add globalization/localization services
builder.Services.AddGlobalization();
builder.Services.AddSingleton<IGlobalizationSettings, GlobalizationSettings>();

// Add IHttpContextAccessor as singleton (required for Blazor server-side)
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add controllers WITHOUT API versioning (simpler routes)
builder.Services.AddControllers();

// Register FluentValidation validators from the Globalization assembly. Singleton because the
// validators are stateless — rules are built in their constructors and messages resolve the culture
// at validation time — and the Singleton WebSiteHostingService injects one, which a Scoped
// registration would make a captive dependency.
// Services call IValidator<T> explicitly rather than using FluentValidation.AspNetCore's model-binding
// integration, which its author deprecated; explicit validation also keeps failures inside the
// Result pattern instead of short-circuiting to a ProblemDetails response.
builder.Services.AddValidatorsFromAssemblyContaining<SharedResource>(lifetime: ServiceLifetime.Singleton);

// Add services to the container.
builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();

// Register file reader abstraction (singleton - stateless file system wrapper)
builder.Services.AddSingleton<IFileReader, SystemFileReader>();

// Register DSM settings service (singleton - reads /etc/synoinfo.conf once at startup)
builder.Services.AddSingleton<IDsmSettingsService, DsmSettingsService>();

// Register DSM API client and authentication facade
builder.Services.AddSingleton<DsmApiClient>();
builder.Services.AddScoped<IDsmSession, DsmSession>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register platform info service (singleton - platform detection happens once at startup)
builder.Services.AddSingleton<PlatformInfoService>();

// Register file manager service with configured root path for runtimes
builder.Services.AddScoped<IFileManagerService>(sp => new FileManagerService(sp.GetRequiredService<ILogger<ILogFileManagerService>>(), ApplicationConstants.RuntimesRootPath));

// Register archive extractor service (Scoped - depends on Scoped IFileManagerService)
builder.Services.AddScoped<IArchiveExtractorService, ArchiveExtractorService>();

// Register downloader service (Scoped - depends on Scoped IFileManagerService)
builder.Services.AddScoped<IDownloaderService, DownloaderService>();

// Register versions detector service (Singleton - caches expensive dotnet --info output)
builder.Services.AddSingleton<IVersionsDetectorService, VersionsDetectorService>();

// Register assembly runtime detector (Singleton - depends on IVersionsDetectorService)
builder.Services.AddSingleton<IAssemblyRuntimeDetector, AssemblyRuntimeDetector>();

// Register process runner (Singleton - stateless process spawning abstraction)
builder.Services.AddSingleton<IProcessRunner, SystemProcessRunner>();

// Register services for runtime/framework management
builder.Services.AddScoped<IDotnetVersionService, DotnetVersionService>();
builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();

// Register file system service (Scoped - depends on Scoped DsmSession)
builder.Services.AddScoped<IFileSystemService, FileSystemService>();

// Register log download service
builder.Services.AddScoped<ILogDownloadService, LogDownloadService>();

// Register website hosting services
builder.Services.AddScoped<IReverseProxyManagerService, ReverseProxyManagerService>();
builder.Services.AddSingleton<WebSitesConfigurationService>();
builder.Services.AddSingleton<IWebSiteHostingService, WebSiteHostingService>();
builder.Services.AddSingleton(sp => (IHostedService)sp.GetRequiredService<IWebSiteHostingService>());

// DSM's nginx proxies /adwh to this process over loopback, so without this every request reports
// 127.0.0.1 and the login throttle below would partition everyone into one bucket. nginx appends the
// peer to X-Forwarded-For, placing the real address last; ForwardLimit stays at its default of 1 so
// only that last entry is read and a client-supplied prefix cannot spoof the address.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});

// Rate limiting for login endpoint (brute-force protection), partitioned per client address
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(ApplicationConstants.RateLimitPolicyLogin, LoginRateLimitPolicy.Partition);
});

var app = builder.Build();

// Wire system culture from DSM settings (no auth needed)
app.ApplyDsmSystemCulture();

// Apply path base FIRST - before any middleware that needs to know about the prefix
app.UsePathBase(ApplicationConstants.ApplicationUrlSubPath);

// Must precede any middleware reading the client address — notably the login rate limiter
app.UseForwardedHeaders();

// Request localization must be early in the pipeline (after path base, before routing)
app.UseGlobalizationRequestLocalization();

// Request tracking must be early to capture ID for the full pipeline
app.UseMiddleware<RequestTrackingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Rate limiter must be before status code pages to prevent 429 from being re-executed to /not-found
app.UseRateLimiter();

app.UseStatusCodePagesWithReExecute("/not-found?status={0}", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Security headers
app.Use((context, next) =>
{
    context.Response.Headers.Append(SecurityHeaders.XContentTypeOptionsName, SecurityHeaders.XContentTypeOptions);
    context.Response.Headers.Append(SecurityHeaders.XFrameOptionsName, SecurityHeaders.XFrameOptions);
    context.Response.Headers.Append(SecurityHeaders.ReferrerPolicyName, SecurityHeaders.ReferrerPolicy);
    context.Response.Headers.Append(SecurityHeaders.ContentSecurityPolicyName, SecurityHeaders.ContentSecurityPolicy);
    context.Response.Headers.Append(SecurityHeaders.XXssProtectionName, SecurityHeaders.XXssProtection);
    return next();
});

// Session middleware must be before antiforgery and controllers
app.UseSession();

app.UseRouting();

app.MapControllers();
app.MapErrorEndpoints();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode()
                             .AddAdditionalAssemblies(typeof(Askyl.Dsm.WebHosting.Ui.Client._Imports).Assembly);

app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() => Log.CloseAndFlush());

app.Run();
