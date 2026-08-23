using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    // ----- 모드 전환/시작 시 공유 위치(CurrentIndex)로 스크롤 (06-view-modes.md 공통 절, 02-architecture.md 세션 복원) -----

    private void Root_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is ContinuousPageViewModel viewModel)
        {
            ScrollToIndex(viewModel.CurrentIndex);
        }
    }

    private void ScrollToIndex(int index)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (ImagesItemsControl.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement container)
            {
                container.BringIntoView();
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    // ----- 이미지 추가 (탐색기/브라우저 드롭) -----

    private void Grid_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = ImageDropHelper.CanAccept(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Grid_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not ContinuousPageViewModel viewModel)
        {
            return;
        }

        var inputs = ImageDropHelper.ExtractInputs(e.Data);
        if (inputs.Count > 0)
        {
            viewModel.AddDroppedImages(inputs);
        }
    }

    // ----- 확대/축소 (Ctrl+스크롤) -----

    private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control || DataContext is not ContinuousPageViewModel viewModel)
        {
            return;
        }

        if (e.Delta > 0)
        {
            viewModel.ZoomInCommand.Execute(null);
        }
        else
        {
            viewModel.ZoomOutCommand.Execute(null);
        }

        e.Handled = true;
    }

    // ----- 페이지 내 순서 변경 (드래그 재정렬) -----

    private void ImageItemBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);

        if (sender is IInputElement element)
        {
            Keyboard.Focus(element);
        }

        if (DataContext is ContinuousPageViewModel viewModel
            && sender is FrameworkElement { DataContext: ImageItem item })
        {
            viewModel.SetCurrentIndex(item);
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
