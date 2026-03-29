using Microsoft.Extensions.Logging;

using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Runtime;

Console.WriteLine("Starting");

FileManager.Initialize();

// Create a simple console logger for the standalone application
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<PlatformInfoService>();

var platformInfo = new PlatformInfoService(logger);
var downloader = new Downloader(platformInfo);

var fileName = await downloader.DownloadToAsync(true, CancellationToken.None);
ArchiveExtractor.Decompress(fileName);

Console.WriteLine("Done");
