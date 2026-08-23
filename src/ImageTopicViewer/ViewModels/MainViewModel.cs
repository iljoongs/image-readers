using CommunityToolkit.Mvvm.ComponentModel;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _dataFolderPath;

    public MainViewModel(ISettingsService settingsService)
    {
        _dataFolderPath = settingsService.Settings.DataFolderPath ?? string.Empty;
    }
}
