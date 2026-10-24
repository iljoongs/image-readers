using System.IO;
using System.Text.Json;
using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

/// <summary>`SettingsService`와 같은 패턴으로 `%APPDATA%\ImageTopicViewer\management.json`을 읽고 쓴다.
/// settings.json(세션/환경설정)과 내용을 분리해, 소주제 업데이트 날짜 같은 "관리" 데이터를 따로 둔다
/// (04-topic-management.md "관리").</summary>
public class ManagementService : IManagementService
{
    private static readonly string ManagementFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ImageTopicViewer",
        "management.json");

    public TopicManagementData Data { get; }

    public ManagementService()
    {
        Data = Load();
    }

    private static TopicManagementData Load()
    {
        if (!File.Exists(ManagementFilePath))
        {
            return new TopicManagementData();
        }

        try
        {
            var json = File.ReadAllText(ManagementFilePath);
            return JsonSerializer.Deserialize<TopicManagementData>(json) ?? new TopicManagementData();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return new TopicManagementData();
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(ManagementFilePath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ManagementFilePath, json);
    }
}
