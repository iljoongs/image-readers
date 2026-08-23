namespace ImageTopicViewer.Models;

public class AppSettings
{
    public string? DataFolderPath { get; set; }

    // 창 상태 (02-architecture.md "세션 상태 저장/복원")
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    // 마지막 세션 상태
    public string? LastMajorTopicName { get; set; }
    public string? LastMinorTopicName { get; set; }
    public bool LastIsSingleView { get; set; }
    public int LastImageIndex { get; set; }
}
