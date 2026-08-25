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
            var majorNode = new TopicNode(Path.GetFileName(majorDir), majorDir, isMajorTopic: true);

            foreach (var minorDir in Directory.GetDirectories(majorDir).OrderBy(Path.GetFileName))
            {
                majorNode.Children.Add(new TopicNode(Path.GetFileName(minorDir), minorDir, isMajorTopic: false));
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

        return new TopicNode(name, path, isMajorTopic: true);
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

        var node = new TopicNode(name, path, isMajorTopic: false);
        majorTopic.Children.Add(node);
        return node;
    }

    public void DeleteTopic(TopicNode node)
    {
        RecycleBin.Send(node.FullPath);
    }

    public void RenameTopic(TopicNode node, string newName)
    {
        var parentDirPath = Directory.GetParent(node.FullPath)!.FullName;
        var siblingNames = Directory.GetDirectories(parentDirPath)
            .Select(Path.GetFileName)
            .Where(n => !string.Equals(n, node.Name, StringComparison.Ordinal));

        if (!TopicNameValidator.IsValid(newName, siblingNames!, out var error))
        {
            throw new ArgumentException(error);
        }

        if (node.IsMajorTopic)
        {
            RenameMajorTopic(node, newName);
        }
        else
        {
            RenameMinorTopic(node, newName);
        }
    }

    private static void RenameMajorTopic(TopicNode majorNode, string newName)
    {
        var oldName = majorNode.Name;
        var oldPath = majorNode.FullPath;
        var newPath = Path.Combine(Path.GetDirectoryName(oldPath)!, newName);

        Directory.Move(oldPath, newPath);

        majorNode.Name = newName;
        majorNode.FullPath = newPath;

        foreach (var minorNode in majorNode.Children)
        {
            var newMinorPath = Path.Combine(newPath, minorNode.Name);
            minorNode.FullPath = newMinorPath;

            RenameImageFilePrefix(
                newMinorPath,
                oldPrefix: $"{oldName}_{minorNode.Name}_",
                newPrefix: $"{newName}_{minorNode.Name}_");
        }
    }

    private static void RenameMinorTopic(TopicNode minorNode, string newName)
    {
        var oldName = minorNode.Name;
        var oldPath = minorNode.FullPath;
        var majorName = Directory.GetParent(oldPath)!.Name;
        var newPath = Path.Combine(Path.GetDirectoryName(oldPath)!, newName);

        Directory.Move(oldPath, newPath);

        minorNode.Name = newName;
        minorNode.FullPath = newPath;

        RenameImageFilePrefix(
            newPath,
            oldPrefix: $"{majorName}_{oldName}_",
            newPrefix: $"{majorName}_{newName}_");
    }

    /// <summary>폴더 내 이미지 파일명의 {대주제}_{소주제}_ 접두사를 새 이름으로 일괄 재작성한다. 번호 부분은 그대로 유지된다.</summary>
    private static void RenameImageFilePrefix(string folderPath, string oldPrefix, string newPrefix)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }

        foreach (var filePath in ImageFileExtensions.EnumerateImageFiles(folderPath))
        {
            var fileName = Path.GetFileName(filePath);
            if (!fileName.StartsWith(oldPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var suffix = fileName[oldPrefix.Length..];
            var newPath = Path.Combine(folderPath, newPrefix + suffix);
            File.Move(filePath, newPath);
        }
    }
}
