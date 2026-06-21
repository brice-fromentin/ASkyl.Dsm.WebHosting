using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class TreeContentServiceTests
{
    private readonly Mock<IFileSystemService> _fileSystemService;
    private readonly Mock<ILocalizer> _localizer;

    public TreeContentServiceTests()
    {
        _fileSystemService = new Mock<IFileSystemService>();
        _localizer = new Mock<ILocalizer>();
    }

    private TreeContentService CreateService()
        => new(_fileSystemService.Object, _localizer.Object);

    #region Successful Directory Loading

    [Fact]
    public async Task LoadChildDirectoriesAsync_Success_ReturnsTreeViewItems()
    {
        // Arrange
        var contents = new List<FsEntry>
        {
            new("/path/dir1", "dir1", true, "/path/dir1", 0, DateTime.UtcNow),
            new("/path/dir2", "dir2", true, "/path/dir2", 0, DateTime.UtcNow),
        };
        _fileSystemService.Setup(f => f.GetDirectoryContentsAsync("/path", true))
            .ReturnsAsync(DirectoryContentsResult.CreateSuccess(contents));
        var service = CreateService();

        // Act
        var result = await service.LoadChildDirectoriesAsync("/path", _ => Task.CompletedTask, _ => Task.FromResult(new List<TreeViewItem>()));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("dir1", result[0].Text);
        Assert.Equal("dir2", result[1].Text);
    }

    #endregion

    #region Empty Directory

    [Fact]
    public async Task LoadChildDirectoriesAsync_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        _fileSystemService.Setup(f => f.GetDirectoryContentsAsync("/empty", true))
            .ReturnsAsync(DirectoryContentsResult.CreateSuccess([]));
        var service = CreateService();

        // Act
        var result = await service.LoadChildDirectoriesAsync("/empty", _ => Task.CompletedTask, _ => Task.FromResult(new List<TreeViewItem>()));

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Failed API Call

    [Fact]
    public async Task LoadChildDirectoriesAsync_Failure_CallsErrorHandler()
    {
        // Arrange
        _fileSystemService.Setup(f => f.GetDirectoryContentsAsync("/path", true))
            .ReturnsAsync(DirectoryContentsResult.CreateFailure("API error"));
        bool errorHandlerCalled = false;
        var service = CreateService();

        // Act
        var result = await service.LoadChildDirectoriesAsync("/path", async _ => { errorHandlerCalled = true; }, _ => Task.FromResult(new List<TreeViewItem>()));

        // Assert
        Assert.True(errorHandlerCalled);
        Assert.Empty(result);
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task LoadChildDirectoriesAsync_ThrowsException_CallsErrorHandler()
    {
        // Arrange
        _fileSystemService.Setup(f => f.GetDirectoryContentsAsync("/path", true))
            .ThrowsAsync(new InvalidOperationException("Network error"));
        bool errorHandlerCalled = false;
        var service = CreateService();

        // Act
        var result = await service.LoadChildDirectoriesAsync("/path", async _ => { errorHandlerCalled = true; }, _ => Task.FromResult(new List<TreeViewItem>()));

        // Assert
        Assert.True(errorHandlerCalled);
        Assert.Empty(result);
    }

    #endregion

    #region Lazy Loading Configuration

    [Fact]
    public async Task LoadChildDirectoriesAsync_ReturnsItemsWithLazyLoading()
    {
        // Arrange
        var contents = new List<FsEntry>
        {
            new("/path/dir1", "dir1", true, "/path/dir1", 0, DateTime.UtcNow),
        };
        _fileSystemService.Setup(f => f.GetDirectoryContentsAsync("/path", true))
            .ReturnsAsync(DirectoryContentsResult.CreateSuccess(contents));

        List<string> expandedItems = [];
        var loadChildren = new Func<string, Task<List<TreeViewItem>>>(async id =>
        {
            expandedItems.Add(id);
            return [new TreeViewItem(id, "child", [])];
        });

        var service = CreateService();

        // Act
        var result = await service.LoadChildDirectoriesAsync("/path", _ => Task.CompletedTask, loadChildren);

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].OnExpandedAsync);
    }

    #endregion
}
