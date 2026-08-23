using System.Collections.ObjectModel;
using System.IO;
using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public class FileSystemTopicRepository : ITopicRepository
{
    private readonly string _dataFolderPath;

    public FileSystemTopicRepository(string dataFolderPath)
    {
        _dataFolderPath = dataFolderPath;
    }

    public ObservableCollection<TopicNode> GetTopics()
    {
        var topics = new ObservableCollection<TopicNode>();

        if (!Directory.Exists(_dataFolderPath))
        {
            return topics;
        }

        foreach (var majorDir in Directory.GetDirectories(_dataFolderPath).OrderBy(Path.GetFileName))
        {
            var majorNode = new TopicNode
            {
                Name = Path.GetFileName(majorDir),
                FullPath = majorDir,
                IsMajorTopic = true,
            };

            foreach (var minorDir in Directory.GetDirectories(majorDir).OrderBy(Path.GetFileName))
            {
                majorNode.Children.Add(new TopicNode
                {
                    Name = Path.GetFileName(minorDir),
                    FullPath = minorDir,
                    IsMajorTopic = false,
                });
            }

            topics.Add(majorNode);
        }

        return topics;
    }

    public TopicNode CreateMajorTopic(string name)
    {
        var siblingNames = Directory.Exists(_dataFolderPath)
            ? Directory.GetDirectories(_dataFolderPath).Select(Path.GetFileName)!
            : Enumerable.Empty<string>();

        if (!TopicNameValidator.IsValid(name, siblingNames!, out var error))
        {
            throw new ArgumentException(error);
        }

        var path = Path.Combine(_dataFolderPath, name);
        Directory.CreateDirectory(path);

        return new TopicNode { Name = name, FullPath = path, IsMajorTopic = true };
    }

    public TopicNode CreateMinorTopic(TopicNode majorTopic, string name)
    {
        var siblingNames = Directory.GetDirectories(majorTopic.FullPath).Select(Path.GetFileName);

        if (!TopicNameValidator.IsValid(name, siblingNames!, out var error))
        {
            throw new ArgumentException(error);
        }

        var path = Path.Combine(majorTopic.FullPath, name);
        Directory.CreateDirectory(path);

        var node = new TopicNode { Name = name, FullPath = path, IsMajorTopic = false };
        majorTopic.Children.Add(node);
        return node;
    }
}
