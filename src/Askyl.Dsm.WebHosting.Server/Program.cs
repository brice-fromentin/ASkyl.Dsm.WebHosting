using System.Text.Json.Serialization;
using Microsoft.AspNetCore.StaticFiles;

// Determine the folder to use for wwwroot based on the current environment
var wwwroot = Path.Combine(Environment.CurrentDirectory, "wwwroot");
if (File.Exists(Path.Combine(AppContext.BaseDirectory, "wwwroot/index.html")))
{
    wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
}

// Create Builder and configure
var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { WebRootPath = wwwroot });

builder.Services.AddResponseCompression(options => { options.EnableForHttps = true;  });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// Create Application
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("WebRoot : {wwwroot}", wwwroot);

app.UseResponseCompression();

// Add MIME type to allow Blazor WASM to work
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".dat"] = "application/dat";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });

app.UseDefaultFiles();
app.MapFallbackToFile("index.html");

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
