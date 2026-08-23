using System.Windows;
using System.Windows.Controls;
using ImageTopicViewer.Models;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class TopicTreeView : UserControl
{
    public TopicTreeView()
    {
        InitializeComponent();
    }

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is TopicTreeViewModel viewModel)
        {
            viewModel.SelectedNode = e.NewValue as TopicNode;
        }
    }

    private void AddMinorTopicMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TopicTreeViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.AddMinorTopicCommand.Execute(node);
        }
    }
}
