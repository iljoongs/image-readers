using System.IO;
using System.Windows.Media.Imaging;

namespace ImageTopicViewer.Services;

/// <summary>드롭된 이미지의 출처. 로컬 파일이면 저장 후 원본을 삭제하고, 이미 디코딩된 비트맵이면 그대로 인코딩만 하며, 스트림(예: 브라우저의 가상 파일 드래그)이면 디코딩부터 한다.</summary>
public abstract record ImageSourceInput
{
    public sealed record FromFile(string SourceFilePath) : ImageSourceInput;

    public sealed record FromBitmap(BitmapSource Bitmap) : ImageSourceInput;

    /// <summary>SuggestedFileName은 원본 확장자를 판단하는 데 쓰인다(예: 브라우저 가상 파일의 파일명 힌트). 없으면 PNG로 대체 저장된다.</summary>
    public sealed record FromStream(Stream Content, string? SuggestedFileName) : ImageSourceInput;
}
