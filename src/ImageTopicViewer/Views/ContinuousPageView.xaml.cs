using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageTopicViewer.Models;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class ContinuousPageView : UserControl
{
    private static readonly Brush DropHighlightBrush = new SolidColorBrush(Color.FromArgb(60, 0, 120, 215));

    private Point _dragStartPoint;

    public ContinuousPageView()
    {
        InitializeComponent();
    }

    // ----- 이미지 추가 (탐색기/브라우저 드롭) -----

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

    // ----- 페이지 내 순서 변경 (드래그 재정렬) -----

    private void ImageItemBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        if (sender is IInputElement element)
        {
            Keyboard.Focus(element);
        }
    }

    private void ImageItemBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: ImageItem item } element)
        {
            return;
        }

        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(element, item, DragDropEffects.Move);
    }

    private void ImageItemBorder_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ImageItem)))
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private void ImageItemBorder_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ImageItem)) && sender is Border border)
        {
            border.BorderBrush = DropHighlightBrush;
        }
    }

    private void ImageItemBorder_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.BorderBrush = Brushes.Transparent;
        }
    }

    private void ImageItemBorder_Drop(object sender, DragEventArgs e)
    {
        if (sender is Border border)
        {
            border.BorderBrush = Brushes.Transparent;
        }

        if (DataContext is not ContinuousPageViewModel viewModel)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: ImageItem targetItem })
        {
            return;
        }

        if (e.Data.GetData(typeof(ImageItem)) is not ImageItem draggedItem)
        {
            return;
        }

        viewModel.MoveImage(draggedItem, targetItem);
        e.Handled = true;
    }

    // ----- 개별 이미지 삭제 -----

    private void ImageItemBorder_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: ImageItem item }
            || DataContext is not ContinuousPageViewModel viewModel)
        {
            return;
        }

        e.Handled = true;
        viewModel.RequestDeleteImage(item);
    }

    private void DeleteImageMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ImageItem item }
            || DataContext is not ContinuousPageViewModel viewModel)
        {
            return;
        }

        viewModel.RequestDeleteImage(item);
    }
}
