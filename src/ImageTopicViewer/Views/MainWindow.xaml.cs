using System.Windows;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
