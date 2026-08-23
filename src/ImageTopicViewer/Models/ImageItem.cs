using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageTopicViewer.Models;

public partial class ImageItem : ObservableObject
{
    public string FullPath { get; private set; }
    public string FileName { get; private set; }

    [ObservableProperty]
    private ImageSource? _image;

    public ImageItem(string fullPath, string fileName)
    {
        FullPath = fullPath;
        FileName = fileName;
    }

    /// <summary>재넘버링(순서 변경/삭제) 후 파일이 새 이름으로 옮겨졌을 때 호출한다. 이미 로드된 Image는 그대로 유지된다.</summary>
    public void UpdatePath(string fullPath, string fileName)
    {
        FullPath = fullPath;
        FileName = fileName;
    }
}
