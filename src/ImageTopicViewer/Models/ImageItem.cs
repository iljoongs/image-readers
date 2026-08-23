using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageTopicViewer.Models;

public partial class ImageItem : ObservableObject
{
    public required string FullPath { get; init; }
    public required string FileName { get; init; }

    [ObservableProperty]
    private ImageSource? _image;
}
