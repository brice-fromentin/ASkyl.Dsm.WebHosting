using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Ui.Services;

namespace Askyl.Dsm.WebHosting.Ui.Models;

public sealed class DirectoryTreeNode : ITreeViewItem
{
    private static readonly Icon FolderIcon = new Icons.Regular.Size16.Folder();
    private static readonly Icon SharedFolderIcon = new Icons.Regular.Size16.FolderOpen();

    private readonly IFileNavigationService _fileNavigationService;
    private readonly Func<string, Task> _errorHandler;

    public string Path { get; set; } = default!;
    public bool IsLoading { get; set; }
    public bool IsSharedFolder { get; set; }

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

    private DirectoryTreeNode(string text, string path, bool isSharedFolder, IFileNavigationService fileNavigationService, Func<string, Task> errorHandler)
    {
        Text = text;
        Path = path;
        IsSharedFolder = isSharedFolder;
        Items = TreeViewItem.LoadingTreeViewItems;
        _fileNavigationService = fileNavigationService;
        _errorHandler = errorHandler;
    }

    private async Task LoadChildrenAsync(TreeViewItemExpandedEventArgs args)
    {
        try
        {
            IsLoading = true;
            var children = await _fileNavigationService.GetDirectoryChildrenAsync(Path, _errorHandler);
            Items = children;
        }
        catch (FileStationApiException ex)
        {
            Items = [];
            var errorMessage = $"Failed to load folder '{Text}': {ex.FormattedMessage}";
            await _errorHandler(errorMessage);
        }
        catch (Exception ex)
        {
            Items = [];
            var errorMessage = $"Failed to load folder '{Text}': {ex.Message}";
            await _errorHandler(errorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public static DirectoryTreeNode FromFileStationFile(FileStationFile file, IFileNavigationService fileNavigationService, Func<string, Task> errorHandler)
    {
        if (!file.IsDirectory && file.Type != "dir")
        {
            throw new ArgumentException("FileStationFile must be a directory", nameof(file));
        }

        return new(file.Name, file.Path, false, fileNavigationService, errorHandler);
    }

    public static DirectoryTreeNode CreateSharedFolder(string name, string path, IFileNavigationService fileNavigationService, Func<string, Task> errorHandler) =>
        new(name, path, true, fileNavigationService, errorHandler);
}
