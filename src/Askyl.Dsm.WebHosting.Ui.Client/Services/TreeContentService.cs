using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Ui.Client.Interfaces;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Implementation of ITreeContentService that loads directory content from the server.
/// </summary>
/// <param name="fileSystemService">The file system service to fetch directory contents.</param>
public class TreeContentService(IFileSystemService fileSystemService) : ITreeContentService
{
    /// <inheritdoc/>
    public async Task<List<TreeViewItem>> LoadChildDirectoriesAsync(string path, Func<string, Task> errorHandler, Func<string, Task<List<TreeViewItem>>> loadChildrenAsync)
    {
        try
        {
            // Use directoryOnly=true to get only directories - no client-side filtering needed!
            var contentsResult = await fileSystemService.GetDirectoryContentsAsync(path, true);

            if (!contentsResult.Success)
            {
                await errorHandler($"{ApplicationConstants.FailedToLoadDirectoryContentsErrorMessage}: {contentsResult.Message}");
                return [];
            }

            var contents = contentsResult.Value;

            if (contents is null or { Count: 0 })
            {
                return [];
            }

            // All items are already directories - no filtering needed!
            return [.. contents.Select(f => CreateTreeViewItemWithLazyLoading(f.Path, f.Name, loadChildrenAsync))];
        }
        catch (Exception ex)
        {
            await errorHandler($"{ApplicationConstants.FailedToLoadDirectoryContentsErrorMessage}: {ex.Message}");

            return [];
        }
    }

    private static TreeViewItem CreateTreeViewItemWithLazyLoading(string path, string name, Func<string, Task<List<TreeViewItem>>> loadChildrenAsync)
    {
        return new(path, name, TreeViewItem.LoadingTreeViewItems)
        {
            OnExpandedAsync = async args =>
            {
                var result = await loadChildrenAsync(args.CurrentItem.Id);
                args.CurrentItem.Items = result ?? TreeViewItem.LoadingTreeViewItems;
            }
        };
    }
}
