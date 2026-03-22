using Askyl.Dsm.WebHosting.Tools.Runtime;

Console.WriteLine("Starting");

FileSystem.Initialize();

var fileName = await Downloader.DownloadToAsync(true);
GzUnTar.Decompress(fileName);

Console.WriteLine("Done");
