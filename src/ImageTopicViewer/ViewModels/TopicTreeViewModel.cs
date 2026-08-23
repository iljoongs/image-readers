using System.Collections.ObjectModel;
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

    private static string? Validate(string name, IEnumerable<string> siblingNames)
    {
        TopicNameValidator.IsValid(name, siblingNames, out var error);
        return error;
    }
}
