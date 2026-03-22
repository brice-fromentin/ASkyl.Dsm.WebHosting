using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Client.Interfaces;

/// <summary>
/// Service for loading directory content as TreeViewItem objects with lazy loading support.
/// </summary>
public interface ITreeContentService
{
    /// <summary>
    /// Loads child directories for a given path and converts them to TreeViewItem objects.
    /// Each returned item will have OnExpandedAsync set to enable nested lazy loading.
    /// </summary>
    /// <param name="path">The directory path to load.</param>
    /// <param name="errorHandler">Callback for error handling.</param>
    /// <param name="loadChildrenAsync">Callback to load subdirectories (for nested lazy loading).</param>
    /// <returns>A list of TreeViewItem objects representing subdirectories with lazy loading enabled.</returns>
    Task<List<TreeViewItem>> LoadChildDirectoriesAsync(string path, Func<string, Task> errorHandler, Func<string, Task<List<TreeViewItem>>> loadChildrenAsync);
}
