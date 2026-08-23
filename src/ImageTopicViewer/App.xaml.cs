using System.IO;
using System.Windows;
using ImageTopicViewer.Services;
using ImageTopicViewer.ViewModels;
using ImageTopicViewer.Views;

namespace ImageTopicViewer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();

        if (string.IsNullOrEmpty(settingsService.Settings.DataFolderPath)
            || !Directory.Exists(settingsService.Settings.DataFolderPath))
        {
            var picker = new DataFolderPickerDialog();
            if (picker.ShowDialog() != true || string.IsNullOrEmpty(picker.SelectedFolderPath))
            {
                Shutdown();
                return;
            }

            settingsService.Settings.DataFolderPath = picker.SelectedFolderPath;
            settingsService.Save();
        }

        var topicRepository = new FileSystemTopicRepository(settingsService.Settings.DataFolderPath!);
        var mainViewModel = new MainViewModel(topicRepository);
        var mainWindow = new MainWindow(mainViewModel);
        mainWindow.Show();
    }
}
