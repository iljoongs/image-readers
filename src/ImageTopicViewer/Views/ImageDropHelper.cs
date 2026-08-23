using System.Windows;
using System.Windows.Media.Imaging;
using ImageTopicViewer.Services;

namespace ImageTopicViewer.Views;

/// <summary>ContinuousPageView/SingleImageView가 공유하는 드래그드롭 판별/추출 로직 (05-image-features.md).</summary>
internal static class ImageDropHelper
{
    public static bool CanAccept(IDataObject data)
    {
        return data.GetDataPresent(DataFormats.FileDrop)
            || data.GetDataPresent(DataFormats.Bitmap)
            || VirtualFileDropReader.HasVirtualFiles(data);
    }

    public static List<ImageSourceInput> ExtractInputs(IDataObject data)
    {
        var inputs = new List<ImageSourceInput>();

        if (data.GetDataPresent(DataFormats.FileDrop) && data.GetData(DataFormats.FileDrop) is string[] filePaths)
        {
            inputs.AddRange(filePaths.Select(path => (ImageSourceInput)new ImageSourceInput.FromFile(path)));
        }
        else if (data.GetDataPresent(DataFormats.Bitmap) && data.GetData(DataFormats.Bitmap) is BitmapSource bitmap)
        {
            inputs.Add(new ImageSourceInput.FromBitmap(bitmap));
        }
        else
        {
            // 웹 브라우저 등 실제 로컬 파일이 없는 "가상 파일" 드래그.
            var streams = VirtualFileDropReader.ReadImageStreams(data);
            inputs.AddRange(streams.Select(s => (ImageSourceInput)new ImageSourceInput.FromStream(s)));
        }

        return inputs;
    }
}
