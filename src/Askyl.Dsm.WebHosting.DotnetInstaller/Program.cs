using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Tools.Runtime;

Console.WriteLine("Starting");

FileManager.Initialize();

var fileName = await Downloader.DownloadToAsync(true);
ArchiveExtractor.Decompress(fileName);

Console.WriteLine("Done");
