using System.Collections.Concurrent;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

public static class FileSystem
{
    private static readonly string _baseDir = AppContext.BaseDirectory;
    private static string _dotnetRoot = "";
    private static readonly ConcurrentDictionary<string, string> _existingFolders = [];

    public const string Downloads = "downloads";
    public const string Temp = "temp";

    public static void Initialize(string root = "")
    {
        _dotnetRoot = root;

        GetDirectory(FileSystem.Downloads);
        GetDirectory(FileSystem.Temp);
    }

    public static string GetDirectory(string name)
    {
        return _existingFolders.GetOrAdd(name, key =>
        {
            var path = Path.Combine(_baseDir, _dotnetRoot, key);

            Console.WriteLine("Creating directory " + path);
            Directory.CreateDirectory(path);

            return path;
        });
    }

    public static string GetFullName(string directory, string file)
    {
        var path = GetDirectory(directory);
        return Path.Combine(path, file);
    }
}
