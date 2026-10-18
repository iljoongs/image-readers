using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ImageTopicViewer.Models;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class TopicTreeView : UserControl
{
    public TopicTreeView()
    {
        InitializeComponent();
        Loaded += TopicTreeView_Loaded;
    }

    /// <summary>앱 시작 시 복원된 "최근에 열었던 대주제/소주제"가 보이도록 트리를 스크롤한다 (07-ui-layout.md "좌측 패널").</summary>
    private void TopicTreeView_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= TopicTreeView_Loaded;

        if (DataContext is TopicTreeViewModel { SelectedNode: { } node })
        {
            // 컨테이너(TreeViewItem)는 레이아웃이 끝난 뒤에야 만들어지므로 한 박자 늦춘다.
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => ScrollToNode(node));
        }
    }

    private void ScrollToNode(TopicNode node)
    {
        Tree.UpdateLayout();

        var container = FindContainer(Tree, node);
        if (container is null)
        {
            return;
        }

        container.ApplyTemplate();
        var header = container.Template.FindName("PART_Header", container) as FrameworkElement ?? container;

        var scrollViewer = FindScrollViewer(Tree);
        if (scrollViewer is null)
        {
            header.BringIntoView();
            return;
        }

        // 선택 항목이 세로 가운데쯤 오도록 스크롤한다.
        var headerTop = header.TransformToAncestor(scrollViewer).Transform(new Point(0, 0)).Y;
        var target = scrollViewer.VerticalOffset + headerTop - (scrollViewer.ViewportHeight - header.ActualHeight) / 2;
        scrollViewer.ScrollToVerticalOffset(Math.Max(0, target));
    }

    private static TreeViewItem? FindContainer(ItemsControl parent, TopicNode node)
    {
        if (parent.ItemContainerGenerator.ContainerFromItem(node) is TreeViewItem direct)
        {
            return direct;
        }

        foreach (var item in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem { IsExpanded: true } child)
            {
                var found = FindContainer(child, node);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            var nested = FindScrollViewer(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
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

    private void SetRatingMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TopicTreeViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.SetMajorTopicRatingCommand.Execute(node);
        }
    }

    private void SetGenreMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TopicTreeViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.SetMajorTopicGenreCommand.Execute(node);
        }
    }

    private void SetCoverMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TopicTreeViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.SetMajorTopicCoverCommand.Execute(node);
        }
    }

    private void RenameTopicMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TopicTreeViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.RenameTopicCommand.Execute(node);
        }
    }

    private void DeleteTopicMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TopicTreeViewModel viewModel
            && sender is MenuItem { DataContext: TopicNode node })
        {
            viewModel.DeleteTopicCommand.Execute(node);
        }
    }
}
