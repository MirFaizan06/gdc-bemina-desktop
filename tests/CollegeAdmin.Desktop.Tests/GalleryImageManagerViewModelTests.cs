using System.Linq;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Desktop.ViewModels;
using CollegeAdmin.Infrastructure.Api;

namespace CollegeAdmin.Desktop.Tests;

public class GalleryImageManagerViewModelTests
{
    [Fact]
    public async Task Constructor_LoadsImagesForTheAlbum()
    {
        var apiClient = new FakeApiClient();
        apiClient.GalleryImages.Add(new GalleryImageDto { Id = 1, AlbumId = 5, ImagePath = "/img/a.jpg" });
        var viewModel = new GalleryImageManagerViewModel(apiClient, 5, "Founders Day");

        await Task.Delay(1); // constructor's fire-and-forget LoadAsync

        Assert.Single(viewModel.Images);
        Assert.Equal("/img/a.jpg", viewModel.Images[0].ImagePath);
    }

    [Fact]
    public void CanAddImage_RequiresNonBlankPath()
    {
        var viewModel = new GalleryImageManagerViewModel(new FakeApiClient(), 5, "Album");
        Assert.False(viewModel.AddImageCommand.CanExecute(null));

        viewModel.NewImagePath = "/img/new.jpg";
        Assert.True(viewModel.AddImageCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddImageAsync_AddsTheImage_AndClearsTheForm()
    {
        var apiClient = new FakeApiClient();
        var viewModel = new GalleryImageManagerViewModel(apiClient, 5, "Album")
        {
            NewImagePath = "/img/new.jpg",
            NewImageCaption = "A caption",
        };
        await Task.Delay(1);

        await viewModel.AddImageCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Images);
        Assert.Equal("/img/new.jpg", apiClient.GalleryImages[0].ImagePath);
        Assert.Equal("A caption", apiClient.GalleryImages[0].Caption);
        Assert.Equal("", viewModel.NewImagePath);
        Assert.Equal("", viewModel.NewImageCaption);
    }

    [Fact]
    public async Task DeleteImageAsync_RemovesTheImage()
    {
        var apiClient = new FakeApiClient();
        apiClient.GalleryImages.Add(new GalleryImageDto { Id = 1, AlbumId = 5, ImagePath = "/img/a.jpg" });
        var viewModel = new GalleryImageManagerViewModel(apiClient, 5, "Album");
        await Task.Delay(1);

        await viewModel.DeleteImageCommand.ExecuteAsync(viewModel.Images[0]);

        Assert.Empty(viewModel.Images);
        Assert.Empty(apiClient.GalleryImages);
    }

    [Fact]
    public async Task MoveImageUpAsync_SwapsWithThePreviousImage()
    {
        // Final Convergence Phase P1-8: gallery_images.sort_order could only ever be set at
        // creation time before this — nothing could reorder an existing image.
        var apiClient = new FakeApiClient();
        apiClient.GalleryImages.Add(new GalleryImageDto { Id = 1, AlbumId = 5, ImagePath = "/img/a.jpg", SortOrder = 0 });
        apiClient.GalleryImages.Add(new GalleryImageDto { Id = 2, AlbumId = 5, ImagePath = "/img/b.jpg", SortOrder = 1 });
        var viewModel = new GalleryImageManagerViewModel(apiClient, 5, "Album");
        await Task.Delay(1);

        await viewModel.MoveImageUpCommand.ExecuteAsync(viewModel.Images.Single(i => i.Id == 2));

        Assert.Equal("/img/b.jpg", viewModel.Images[0].ImagePath);
        Assert.Equal("/img/a.jpg", viewModel.Images[1].ImagePath);
    }

    [Fact]
    public async Task MoveImageDownAsync_AtTheBottom_IsANoOp()
    {
        var apiClient = new FakeApiClient();
        apiClient.GalleryImages.Add(new GalleryImageDto { Id = 1, AlbumId = 5, ImagePath = "/img/a.jpg", SortOrder = 0 });
        var viewModel = new GalleryImageManagerViewModel(apiClient, 5, "Album");
        await Task.Delay(1);

        await viewModel.MoveImageDownCommand.ExecuteAsync(viewModel.Images[0]);

        Assert.Equal("/img/a.jpg", viewModel.Images[0].ImagePath);
    }

    [Fact]
    public async Task AddImageAsync_OnApiRequestException_SetsErrorMessage()
    {
        var apiClient = new FakeApiClient { ThrowOnGalleryImages = new ApiRequestException(new ApiErrorPayload
        {
            Code = "VALIDATION_ERROR",
            Message = "imagePath is required.",
        }) };
        var viewModel = new GalleryImageManagerViewModel(apiClient, 5, "Album") { NewImagePath = "/img/x.jpg" };
        await Task.Delay(1);

        await viewModel.AddImageCommand.ExecuteAsync(null);

        Assert.Equal("imagePath is required.", viewModel.ErrorMessage);
    }
}
