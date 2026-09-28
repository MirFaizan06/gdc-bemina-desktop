using System.Collections.ObjectModel;
using CollegeAdmin.Contracts.Api;
using CollegeAdmin.Infrastructure.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CollegeAdmin.Desktop.ViewModels;

/// <summary>
/// Gallery's images are a nested resource under an album (GalleryController::addImage/deleteImage/
/// images) — a different shape from every other module's flat field list, so this gets its own
/// small dedicated ViewModel rather than trying to force it through GenericCreateViewModel. Opened
/// via GenericListView's third "Manage" row action (see GenericListViewModel.CanManage).
/// </summary>
public sealed partial class GalleryImageManagerViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly int _albumId;

    public GalleryImageManagerViewModel(IApiClient apiClient, int albumId, string albumTitle)
    {
        _apiClient = apiClient;
        _albumId = albumId;
        AlbumTitle = albumTitle;
        _ = LoadAsync();
    }

    public string AlbumTitle { get; }
    public ObservableCollection<GalleryImageDto> Images { get; } = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsEmpty => !IsLoading && ErrorMessage is null && Images.Count == 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddImageCommand))]
    private string _newImagePath = "";

    [ObservableProperty]
    private string _newImageCaption = "";

    /// <summary>Final Convergence Phase P0-7: NewImagePath used to be a raw free-text path box with
    /// zero upload mechanism (the audit's own "type a path and hope it's correct" finding) — this
    /// shows the just-uploaded filename once UploadNewImageAsync sets NewImagePath to the real
    /// server-assigned path, matching FormField.DisplayFileName's equivalent for the generic form.</summary>
    [ObservableProperty]
    private string? _newImageFileName;

    /// <summary>Called by the View's code-behind after it reads the picked file's bytes (a View-
    /// layer filesystem concern) — uploads via the same /api/v1/uploads endpoint every other content
    /// module now uses, then stores the resulting server path into NewImagePath.</summary>
    public async Task UploadNewImageAsync(string fileName, byte[] bytes)
    {
        ErrorMessage = null;
        try
        {
            NewImagePath = await _apiClient.UploadFileAsync("gallery", fileName, bytes);
            NewImageFileName = fileName;
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 } ? string.Join(" ", ex.FieldErrors.Values) : ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var images = await _apiClient.GetGalleryImagesAsync(_albumId);
            Images.Clear();
            foreach (var image in images)
            {
                Images.Add(image);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddImage))]
    private async Task AddImageAsync()
    {
        ErrorMessage = null;
        try
        {
            var images = await _apiClient.AddGalleryImageAsync(
                _albumId, NewImagePath.Trim(),
                string.IsNullOrWhiteSpace(NewImageCaption) ? null : NewImageCaption.Trim(),
                Images.Count);

            Images.Clear();
            foreach (var image in images)
            {
                Images.Add(image);
            }
            NewImagePath = "";
            NewImageCaption = "";
            NewImageFileName = null;
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.FieldErrors is { Count: > 0 }
                ? string.Join(" ", ex.FieldErrors.Values)
                : ex.Message;
        }
    }

    private bool CanAddImage() => NewImagePath.Trim().Length > 0;

    /// <summary>Final Convergence Phase P1-8 (docs/claude/FINAL_COMPLETION_TRACKER.md §3): the
    /// tracker's own gap note — gallery_images.sort_order could only ever be set at creation time,
    /// nothing could reorder an existing image. Both directions share one implementation since the
    /// server-side swap logic (GalleryController::moveImage) is identical either way.</summary>
    [RelayCommand]
    private Task MoveImageUpAsync(GalleryImageDto image) => MoveImageAsync(image, "up");

    [RelayCommand]
    private Task MoveImageDownAsync(GalleryImageDto image) => MoveImageAsync(image, "down");

    private async Task MoveImageAsync(GalleryImageDto image, string direction)
    {
        ErrorMessage = null;
        try
        {
            var images = await _apiClient.MoveGalleryImageAsync(_albumId, image.Id, direction);
            Images.Clear();
            foreach (var updated in images)
            {
                Images.Add(updated);
            }
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteImageAsync(GalleryImageDto image)
    {
        ErrorMessage = null;
        try
        {
            var images = await _apiClient.DeleteGalleryImageAsync(_albumId, image.Id);
            Images.Clear();
            foreach (var remaining in images)
            {
                Images.Add(remaining);
            }
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (ApiRequestException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
