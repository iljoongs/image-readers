using System.Collections.ObjectModel;
using System.IO;
using ImageTopicViewer.Models;

namespace ImageTopicViewer.Services;

public class FileSystemTopicRepository : ITopicRepository
{
    private const string ArchiveExtension = ".zip";

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

        foreach (var majorDir in Directory.GetDirectories(_dataFolderPath).OrderBy(Path.GetFileName, NaturalStringComparer.Instance))
        {
            var majorNode = new TopicNode(Path.GetFileName(majorDir), majorDir, isMajorTopic: true);

            foreach (var minorEntry in EnumerateMinorEntries(majorDir))
            {
                majorNode.Children.Add(new TopicNode(minorEntry.Name, minorEntry.Path, isMajorTopic: false)
                {
                    IsArchive = minorEntry.IsArchive,
                });
            }

            topics.Add(majorNode);
        }

        return topics;
    }

    /// <summary>대주제 폴더 바로 아래의 소주제 폴더와 .zip 파일을 함께 자연 정렬해서 열거한다.</summary>
    private static IEnumerable<(string Name, string Path, bool IsArchive)> EnumerateMinorEntries(string majorDirPath)
    {
        var folders = Directory.GetDirectories(majorDirPath)
            .Select(d => (Name: Path.GetFileName(d)!, Path: d, IsArchive: false));

        var archives = Directory.GetFiles(majorDirPath, "*" + ArchiveExtension)
            .Select(f => (Name: Path.GetFileNameWithoutExtension(f)!, Path: f, IsArchive: true));

        return folders.Concat(archives).OrderBy(e => e.Name, NaturalStringComparer.Instance);
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
        var siblingNames = GetMinorSiblingNames(majorTopic.FullPath);

        if (!TopicNameValidator.IsValid(name, siblingNames, out var error))
        {
            throw new ArgumentException(error);
        }

        var path = Path.Combine(majorTopic.FullPath, name);
        Directory.CreateDirectory(path);

        var node = new TopicNode(name, path, isMajorTopic: false);
        majorTopic.Children.Add(node);
        return node;
    }

    /// <summary>대주제 폴더 바로 아래의 소주제 이름 목록(폴더명 + .zip 파일 베이스네임)을 반환한다 — 이름 중복 검증용.</summary>
    private static IEnumerable<string> GetMinorSiblingNames(string majorDirPath)
    {
        var folderNames = Directory.GetDirectories(majorDirPath).Select(d => Path.GetFileName(d)!);
        var archiveNames = Directory.GetFiles(majorDirPath, "*" + ArchiveExtension).Select(f => Path.GetFileNameWithoutExtension(f)!);
        return folderNames.Concat(archiveNames);
    }

    public void DeleteTopic(TopicNode node)
    {
        RecycleBin.Send(node.FullPath);
    }

    public void RenameTopic(TopicNode node, string newName)
    {
        IEnumerable<string> siblingNames;
        if (node.IsMajorTopic)
        {
            var parentDirPath = Directory.GetParent(node.FullPath)!.FullName;
            siblingNames = Directory.GetDirectories(parentDirPath)
                .Select(d => Path.GetFileName(d)!)
                .Where(n => !string.Equals(n, node.Name, StringComparison.Ordinal));
        }
        else
        {
            var majorDirPath = Directory.GetParent(node.FullPath)!.FullName;
            siblingNames = GetMinorSiblingNames(majorDirPath)
                .Where(n => !string.Equals(n, node.Name, StringComparison.Ordinal));
        }

        if (!TopicNameValidator.IsValid(newName, siblingNames, out var error))
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
            var minorFileName = minorNode.IsArchive ? minorNode.Name + ArchiveExtension : minorNode.Name;
            minorNode.FullPath = Path.Combine(newPath, minorFileName);

            if (!minorNode.IsArchive)
            {
                RenameImageFilePrefix(
                    minorNode.FullPath,
                    oldPrefix: $"{oldName}_{minorNode.Name}_",
                    newPrefix: $"{newName}_{minorNode.Name}_");
            }
            // 압축(zip) 소주제는 내부 파일명이 번호만이라(03-data-storage.md) 다시 쓸 이름이 없다.
        }
    }

    private static void RenameMinorTopic(TopicNode minorNode, string newName)
    {
        var oldName = minorNode.Name;
        var oldPath = minorNode.FullPath;
        var majorName = Directory.GetParent(oldPath)!.Name;
        var newFileName = minorNode.IsArchive ? newName + ArchiveExtension : newName;
        var newPath = Path.Combine(Path.GetDirectoryName(oldPath)!, newFileName);

        if (minorNode.IsArchive)
        {
            File.Move(oldPath, newPath);
        }
        else
        {
            Directory.Move(oldPath, newPath);
            RenameImageFilePrefix(
                newPath,
                oldPrefix: $"{majorName}_{oldName}_",
                newPrefix: $"{majorName}_{newName}_");
        }

        minorNode.Name = newName;
        minorNode.FullPath = newPath;
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
