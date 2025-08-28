using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;
using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Microsoft.FluentUI.AspNetCore.Components;
using Askyl.Dsm.WebHosting.Ui.Services;

namespace Askyl.Dsm.WebHosting.Ui.Models;

public sealed class DirectoryTreeNode : ITreeViewItem
{
    private static readonly Icon FolderIcon = new Icons.Regular.Size16.Folder();
    private static readonly Icon SharedFolderIcon = new Icons.Regular.Size16.FolderOpen();

    private IFileNavigationService? _fileNavigationService;

    public string Path { get; set; } = default!;
    public bool IsLoading { get; set; }
    public bool IsSharedFolder { get; set; }
    public FileStationFileAdditional? Additional { get; set; }

    // ITreeViewItem implementation
    public string Id { get => Path; set => Path = value; }
    public string Text { get; set; } = default!;
    public bool Expanded { get; set; }
    public bool Selected { get; set; }
    public bool Disabled { get; set; }
    public Icon? IconCollapsed { get => IsSharedFolder ? SharedFolderIcon : FolderIcon; set { } }
    public Icon? IconExpanded { get => IsSharedFolder ? SharedFolderIcon : FolderIcon; set { } }
    public IEnumerable<ITreeViewItem>? Items { get; set; } = [];
    public Func<TreeViewItemExpandedEventArgs, Task>? OnExpandedAsync { get => LoadChildrenAsync; set { } }

    // Helper property for typed access to children
    public IEnumerable<DirectoryTreeNode> Children => Items?.Cast<DirectoryTreeNode>() ?? [];

    private DirectoryTreeNode(string text, string path, bool isSharedFolder, FileStationFileAdditional? additional, IFileNavigationService? fileNavigationService)
    {
        Text = text;
        Path = path;
        IsSharedFolder = isSharedFolder;
        Additional = additional;
        Items = TreeViewItem.LoadingTreeViewItems;
        _fileNavigationService = fileNavigationService;
    }

    private async Task LoadChildrenAsync(TreeViewItemExpandedEventArgs args)
    {
        if (_fileNavigationService == null)
        {
            return;
        }

        var children = await _fileNavigationService.GetDirectoryChildrenAsync(Path);
        Items = children;
    }

    public static DirectoryTreeNode FromFileStationFile(FileStationFile file, IFileNavigationService? fileNavigationService = null)
    {
        if (!file.IsDirectory && file.Type != "dir")
        {
            throw new ArgumentException("FileStationFile must be a directory", nameof(file));
        }

        return new(file.Name, file.Path, false, file.Additional, fileNavigationService);
    }

    public static DirectoryTreeNode CreateSharedFolder(string name, string path, IFileNavigationService? fileNavigationService = null)
        => new(name, path, true, null, fileNavigationService);
}
