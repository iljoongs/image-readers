using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class ContinuousPageView : UserControl
{
    public ContinuousPageView()
    {
        InitializeComponent();
    }

    private void Grid_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Bitmap)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Grid_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not ContinuousPageViewModel viewModel)
        {
            return;
        }

        if (e.Data.GetDataPresent(DataFormats.FileDrop)
            && e.Data.GetData(DataFormats.FileDrop) is string[] filePaths)
        {
            viewModel.AddDroppedFiles(filePaths);
        }
        else if (e.Data.GetDataPresent(DataFormats.Bitmap)
                 && e.Data.GetData(DataFormats.Bitmap) is BitmapSource bitmap)
        {
            viewModel.AddDroppedBitmap(bitmap);
        }
    }
}
