using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class SingleImageView : UserControl
{
    public SingleImageView()
    {
        InitializeComponent();
    }

    private void Root_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            Root.Focus();
        }
    }

    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ContinuousPageViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Left)
        {
            viewModel.GoToPreviousImageCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            viewModel.GoToNextImageCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void PreviousArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ContinuousPageViewModel viewModel)
        {
            viewModel.GoToPreviousImageCommand.Execute(null);
        }
    }

    private void NextArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ContinuousPageViewModel viewModel)
        {
            viewModel.GoToNextImageCommand.Execute(null);
        }
    }

    // ----- 마우스 휠: Ctrl+휠은 확대/축소, 일반 휠은 이전/다음 이미지 -----

    private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not ContinuousPageViewModel viewModel)
        {
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Delta > 0)
            {
                viewModel.ZoomInCommand.Execute(null);
            }
            else
            {
                viewModel.ZoomOutCommand.Execute(null);
            }
        }
        else if (e.Delta < 0)
        {
            viewModel.GoToNextImageCommand.Execute(null);
        }
        else
        {
            viewModel.GoToPreviousImageCommand.Execute(null);
        }

        e.Handled = true;
    }

    // ----- 빈 페이지일 때도 페이지 전체에서 드롭 가능 (05-image-features.md) -----

    private void Root_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = ImageDropHelper.CanAccept(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Root_Drop(object sender, DragEventArgs e)
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
}
