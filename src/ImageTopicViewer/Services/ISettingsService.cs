using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }

    void Save();
}
