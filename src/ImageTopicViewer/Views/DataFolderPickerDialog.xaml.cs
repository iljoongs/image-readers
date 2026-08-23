using System.Windows;
using Microsoft.Win32;

namespace ImageTopicViewer.Views;

public partial class DataFolderPickerDialog : Window
{
    public string? SelectedFolderPath { get; private set; }

    public DataFolderPickerDialog()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "데이터 폴더 선택"
        };

        if (dialog.ShowDialog(this) == true)
        {
            SelectedFolderPath = dialog.FolderName;
            SelectedPathTextBox.Text = SelectedFolderPath;
            OkButton.IsEnabled = true;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
