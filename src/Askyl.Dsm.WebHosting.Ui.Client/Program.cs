using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient("John", client => { client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); Console.WriteLine(builder.HostEnvironment.BaseAddress); });
builder.Services.AddFluentUIComponents();

await builder.Build().RunAsync();
