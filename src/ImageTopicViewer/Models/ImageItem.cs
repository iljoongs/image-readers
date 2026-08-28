using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageTopicViewer.Models;

public partial class ImageItem : ObservableObject
{
    public string FullPath { get; private set; }
    public string FileName { get; private set; }

    /// <summary>null이면 폴더 기반 파일. 아니면 이 이미지가 들어있는 .zip 파일 경로 (03-data-storage.md).</summary>
    public string? ArchiveFilePath { get; private init; }

    /// <summary>ArchiveFilePath가 설정된 경우에만 유효한 zip 내부 엔트리 이름.</summary>
    public string? ArchiveEntryName { get; private init; }

    public bool IsFromArchive => ArchiveFilePath is not null;

    [ObservableProperty]
    private ImageSource? _image;

    public ImageItem(string fullPath, string fileName)
    {
        FullPath = fullPath;
        FileName = fileName;
    }

    /// <summary>압축(zip) 소주제 안의 이미지 항목을 만든다. 읽기 전용이며 UpdatePath로 옮겨지지 않는다.</summary>
    public static ImageItem FromArchiveEntry(string archiveFilePath, string entryName)
    {
        return new ImageItem($"{archiveFilePath}::{entryName}", entryName)
        {
            ArchiveFilePath = archiveFilePath,
            ArchiveEntryName = entryName,
        };
    }

    /// <summary>재넘버링(순서 변경/삭제) 후 파일이 새 이름으로 옮겨졌을 때 호출한다. 이미 로드된 Image는 그대로 유지된다.</summary>
    public void UpdatePath(string fullPath, string fileName)
    {
        FullPath = fullPath;
        FileName = fileName;
    }
}
