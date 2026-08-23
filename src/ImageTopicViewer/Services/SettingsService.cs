using System.IO;
using System.Text.Json;
using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public class SettingsService : ISettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ImageTopicViewer",
        "settings.json");

    public AppSettings Settings { get; }

    public SettingsService()
    {
        Settings = Load();
    }

    private static AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsFilePath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFilePath, json);
    }
}
