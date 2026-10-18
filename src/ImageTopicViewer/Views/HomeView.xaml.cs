using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageTopicViewer.Models;
using ImageTopicViewer.Services;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    // ----- 확대/축소 (Ctrl+스크롤, 06-view-modes.md의 ContinuousPageView와 동일한 패턴) -----

    private void RootScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || Keyboard.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        if (e.Delta > 0)
        {
            viewModel.ZoomHomeInCommand.Execute(null);
        }
        else
        {
            viewModel.ZoomHomeOutCommand.Execute(null);
        }

        e.Handled = true;
    }

    private void Card_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is FrameworkElement { DataContext: TopicNode node })
        {
            viewModel.OpenMajorTopicCommand.Execute(node);
        }
    }

    // ----- 썸네일 드래그 앤 드롭 (game-platform 프로젝트의 Card_DragOver/Card_Drop과 동일한 방식) -----

    private void Card_DragOver(object sender, DragEventArgs e)
    {
        var acceptable = DragDropImageHelper.CanAccept(e.Data);
        e.Effects = acceptable ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Card_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel
            || sender is not FrameworkElement { DataContext: TopicNode node })
        {
            return;
        }

        var imagePath = DragDropImageHelper.TryGetImagePath(e.Data, out var isTemporary);
        if (imagePath is null)
        {
            return;
        }

        viewModel.TopicTree.ApplyThumbnail(node, imagePath, deleteSource: isTemporary);
        e.Handled = true;
    }

    private void SetRatingMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.TopicTree.SetMajorTopicRatingCommand.Execute(node);
        }
    }

    private void SetGenreMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.TopicTree.SetMajorTopicGenreCommand.Execute(node);
        }
    }

    private void SetThumbnailMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.TopicTree.SetMajorTopicThumbnailCommand.Execute(node);
        }
    }

    private void RenameTopicMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.TopicTree.RenameTopicCommand.Execute(node);
        }
    }

    private void DeleteTopicMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.TopicTree.DeleteTopicCommand.Execute(node);
        }
    }
}
