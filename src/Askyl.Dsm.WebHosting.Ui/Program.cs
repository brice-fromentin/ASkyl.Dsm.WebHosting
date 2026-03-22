using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Ui.Components;
using Askyl.Dsm.WebHosting.Ui.Services;

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
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();

// Add IHttpContextAccessor as singleton (required for Blazor server-side)
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add controllers WITHOUT API versioning (simpler routes)
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

// Add services to the container.
builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();

// Register DSM API client and authentication facade
builder.Services.AddSingleton<DsmApiClient>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Register services for runtime/framework management
builder.Services.AddScoped<IDotnetVersionService, DotnetVersionService>();
builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();

// Register file system service (requires DsmApiClient)
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();

// Register log download service
builder.Services.AddScoped<ILogDownloadService, LogDownloadService>();

// Register website hosting services
builder.Services.AddSingleton<IReverseProxyManagerService, ReverseProxyManagerService>();
builder.Services.AddSingleton<IWebSitesConfigurationService, WebSitesConfigurationService>();
builder.Services.AddSingleton<WebSiteHostingService>();
builder.Services.AddSingleton<IWebSiteHostingService>(sp => sp.GetRequiredService<WebSiteHostingService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<WebSiteHostingService>());

var app = builder.Build();

// Apply path base FIRST - before any middleware that needs to know about the prefix
app.UsePathBase(ApplicationConstants.ApplicationUrlSubPath);

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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Session middleware must be before antiforgery and controllers
app.UseSession();

app.UseRouting();

app.MapControllers();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode()
                             .AddAdditionalAssemblies(typeof(Askyl.Dsm.WebHosting.Ui.Client._Imports).Assembly);

app.Run();
