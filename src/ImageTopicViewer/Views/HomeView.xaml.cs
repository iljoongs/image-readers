using System.Windows;
using System.Windows.Controls;
using ImageTopicViewer.Models;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void Card_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is FrameworkElement { DataContext: TopicNode node })
        {
            viewModel.OpenMajorTopicCommand.Execute(node);
        }
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

    private void SetCoverMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.TopicTree.SetMajorTopicCoverCommand.Execute(node);
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
