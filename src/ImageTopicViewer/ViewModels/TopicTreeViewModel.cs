using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageTopicViewer.Models;
using ImageTopicViewer.Services;
using ImageTopicViewer.Views;

namespace ImageTopicViewer.ViewModels;

public partial class TopicTreeViewModel : ObservableObject
{
    private readonly ITopicRepository _repository;

    public ObservableCollection<TopicNode> Topics { get; }

    [ObservableProperty]
    private TopicNode? _selectedNode;

    public TopicTreeViewModel(ITopicRepository repository)
    {
        _repository = repository;
        Topics = repository.GetTopics();
    }

    [RelayCommand]
    private void AddMajorTopic()
    {
        var siblingNames = Topics.Select(t => t.Name);
        var dialog = new TextInputDialog(
            "대주제 추가",
            "새 대주제 이름을 입력하세요.",
            name => Validate(name, siblingNames))
        {
            Owner = Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var node = _repository.CreateMajorTopic(dialog.InputText);
            Topics.Add(node);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "대주제 추가 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void AddMinorTopic(TopicNode? majorTopic)
    {
        if (majorTopic is null)
        {
            return;
        }

        var siblingNames = majorTopic.Children.Select(t => t.Name);
        var dialog = new TextInputDialog(
            "소주제 추가",
            $"'{majorTopic.Name}'에 추가할 소주제 이름을 입력하세요.",
            name => Validate(name, siblingNames))
        {
            Owner = Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _repository.CreateMinorTopic(majorTopic, dialog.InputText);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "소주제 추가 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void RenameTopic(TopicNode? node)
    {
        if (node is null)
        {
            return;
        }

        var siblingNames = GetSiblingNames(node);
        var dialog = new TextInputDialog(
            "이름 변경",
            $"'{node.Name}'의 새 이름을 입력하세요.",
            name => name == node.Name ? null : Validate(name, siblingNames),
            node.Name)
        {
            Owner = Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true || dialog.InputText == node.Name)
        {
            return;
        }

        try
        {
            _repository.RenameTopic(node, dialog.InputText);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "이름 변경 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "이름 변경 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 이름 변경으로 현재 표시 중인 소주제의 경로가 바뀌었을 수 있으므로 페이지를 다시 로드하도록 알린다.
        var selectionAffected = ReferenceEquals(SelectedNode, node)
            || (node.IsMajorTopic && SelectedNode is not null && node.Children.Contains(SelectedNode));
        if (selectionAffected)
        {
            OnPropertyChanged(nameof(SelectedNode));
        }
    }

    [RelayCommand]
    private void DeleteTopic(TopicNode? node)
    {
        if (node is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            $"'{node.Name}'을(를) 삭제하시겠습니까?\n하위 이미지가 모두 휴지통으로 이동됩니다.",
            "삭제 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _repository.DeleteTopic(node);
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "삭제 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectionAffected = ReferenceEquals(SelectedNode, node)
            || (node.IsMajorTopic && SelectedNode is not null && node.Children.Contains(SelectedNode));

        if (node.IsMajorTopic)
        {
            Topics.Remove(node);
        }
        else
        {
            var parent = Topics.FirstOrDefault(t => t.Children.Contains(node));
            parent?.Children.Remove(node);
        }

        if (selectionAffected)
        {
            SelectedNode = null;
        }
    }

    private IEnumerable<string> GetSiblingNames(TopicNode node)
    {
        var siblingCollection = node.IsMajorTopic
            ? Topics
            : Topics.FirstOrDefault(t => t.Children.Contains(node))?.Children ?? new ObservableCollection<TopicNode>();

        return siblingCollection.Where(n => !ReferenceEquals(n, node)).Select(n => n.Name).ToList();
    }

    private static string? Validate(string name, IEnumerable<string> siblingNames)
    {
        TopicNameValidator.IsValid(name, siblingNames, out var error);
        return error;
    }
}
