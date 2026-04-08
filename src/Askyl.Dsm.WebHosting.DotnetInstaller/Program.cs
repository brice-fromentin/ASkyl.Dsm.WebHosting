using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Runtime;
using Microsoft.Extensions.Logging;

Console.WriteLine("Starting");

// Create a simple console logger for the standalone application
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var platformLogger = loggerFactory.CreateLogger<PlatformInfoService>();
var fileManagerLogger = loggerFactory.CreateLogger<FileManagerService>();

var platformInfo = new PlatformInfoService(platformLogger);
var fileManager = new FileManagerService(fileManagerLogger, String.Empty);
fileManager.Initialize();

var downloader = new DownloaderService(platformInfo, fileManager);
var archiveExtractor = new ArchiveExtractorService(fileManager);

var fileName = await downloader.DownloadToAsync(true, CancellationToken.None);
archiveExtractor.Decompress(fileName);

Console.WriteLine("Done");
