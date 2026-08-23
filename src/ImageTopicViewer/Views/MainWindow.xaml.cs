using System.ComponentModel;
using System.Windows;
using ImageTopicViewer.Services;
using ImageTopicViewer.ViewModels;

namespace ImageTopicViewer.Views;

public partial class MainWindow : Window
{
    private const double DefaultWidth = 1000;
    private const double DefaultHeight = 700;

    private readonly MainViewModel _viewModel;
    private readonly ISettingsService _settingsService;

    public MainWindow(MainViewModel viewModel, ISettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _settingsService = settingsService;

        RestoreWindowBounds();
        Closing += MainWindow_Closing;
    }

    private void RestoreWindowBounds()
    {
        var s = _settingsService.Settings;

        double left = s.WindowLeft ?? Left;
        double top = s.WindowTop ?? Top;
        double width = s.WindowWidth ?? DefaultWidth;
        double height = s.WindowHeight ?? DefaultHeight;

        if (s.WindowWidth is null || !IsRectVisibleOnScreen(left, top, width, height))
        {
            // 저장된 위치가 없거나 현재 모니터 구성에서 화면 밖으로 벗어나면 기본값 사용 (02-architecture.md 복원 예외 처리).
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = DefaultWidth;
            Height = DefaultHeight;
        }
        else
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        if (s.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private static bool IsRectVisibleOnScreen(double left, double top, double width, double height)
    {
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

        // 창 영역이 가상 화면(전체 모니터) 영역과 조금이라도 겹치면 화면 안으로 간주한다.
        return left < virtualRight && left + width > virtualLeft
            && top < virtualBottom && top + height > virtualTop;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        var s = _settingsService.Settings;

        var bounds = WindowState == WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);
        s.WindowLeft = bounds.Left;
        s.WindowTop = bounds.Top;
        s.WindowWidth = bounds.Width;
        s.WindowHeight = bounds.Height;
        s.WindowMaximized = WindowState == WindowState.Maximized;

        _viewModel.CaptureSession(s);

        _settingsService.Save();
    }
}
