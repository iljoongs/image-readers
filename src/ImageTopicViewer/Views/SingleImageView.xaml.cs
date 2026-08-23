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
}
