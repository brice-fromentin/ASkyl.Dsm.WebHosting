using Askyl.Dsm.WebHosting.Data;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Client.Extensions;

/// <summary>
/// Extension methods for converting FsEntry to TreeViewItem with lazy loading support.
/// </summary>
public static class FsEntryExtensions
{
    extension(List<FsEntry> items)
    {
        /// <summary>
        /// Converts a list of FsEntry objects to TreeViewItem objects for use with FluentTreeView.
        /// Each item is configured for lazy loading of child directories.
        /// </summary>
        /// <param name="loadChildrenAsync">Callback to load child directories when folder is expanded.</param>
        /// <returns>A list of TreeViewItem objects ready for UI binding with lazy loading enabled.</returns>
        public List<TreeViewItem> ToTreeViewItems(Func<string, Task<List<TreeViewItem>>> loadChildrenAsync)
            => [.. items.Select(f => f.ToTreeViewItemWithLazyLoading(loadChildrenAsync))];
    }

    extension(FsEntry item)
    {
        /// <summary>
        /// Converts a single FsEntry to a TreeViewItem configured for lazy loading.
        /// </summary>
        /// <param name="loadChildrenAsync">Callback to load child directories when folder is expanded.</param>
        /// <returns>A TreeViewItem configured with lazy loading support.</returns>
        public TreeViewItem ToTreeViewItemWithLazyLoading(Func<string, Task<List<TreeViewItem>>> loadChildrenAsync)
            => new(item.Path, item.Name, TreeViewItem.LoadingTreeViewItems)
            {
                OnExpandedAsync = args => loadChildrenAsync(args.CurrentItem.Id!).ContinueWith(t =>
                {
                    if (t.Result is not null)
                    {
                        args.CurrentItem.Items = [.. t.Result];
                    }

                    return Task.CompletedTask;
                })
            };
    }
}
