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
    /// <summary>대주제 목록(Topics)에 마지막으로 적용된 정렬 기준. 소주제 목록은 항상 이름 기준으로만 정렬된다
    /// (평점은 대주제에만 있는 값이라 소주제 정렬에는 의미가 없음).</summary>
    private enum TopicSortMode
    {
        Name,
        Rating,
    }

    private readonly ITopicRepository _repository;
    private TopicSortMode _activeSortMode = TopicSortMode.Name;

    public ObservableCollection<TopicNode> Topics { get; }

    [ObservableProperty]
    private TopicNode? _selectedNode;

    [ObservableProperty]
    private bool _isSortDescending;

    [ObservableProperty]
    private bool _isRatingSortDescending;

    public string SortToggleLabel => IsSortDescending ? "이름 오름차순 정렬" : "이름 내림차순 정렬";

    public string RatingSortToggleLabel => IsRatingSortDescending ? "평점 오름차순 정렬" : "평점 내림차순 정렬";

    public TopicTreeViewModel(ITopicRepository repository)
    {
        _repository = repository;
        Topics = repository.GetTopics();
    }

    /// <summary>대주제 노드 클릭 → 하위 소주제 목록 펼침/표시 (04-topic-management.md).</summary>
    partial void OnSelectedNodeChanged(TopicNode? value)
    {
        if (value is { IsMajorTopic: true })
        {
            value.IsExpanded = true;
        }
    }

    partial void OnIsSortDescendingChanged(bool value)
    {
        OnPropertyChanged(nameof(SortToggleLabel));

        SortCollection(Topics);
        foreach (var major in Topics)
        {
            SortCollection(major.Children);
        }
    }

    partial void OnIsRatingSortDescendingChanged(bool value)
    {
        OnPropertyChanged(nameof(RatingSortToggleLabel));

        SortTopicsByRating();
    }

    [RelayCommand]
    private void ToggleSortOrder()
    {
        _activeSortMode = TopicSortMode.Name;
        IsSortDescending = !IsSortDescending;
    }

    [RelayCommand]
    private void ToggleRatingSortOrder()
    {
        _activeSortMode = TopicSortMode.Rating;
        IsRatingSortDescending = !IsRatingSortDescending;
    }

    /// <summary>이름 기준으로 정렬하되, ObservableCollection.Move로 재배치해 IsExpanded/IsSelected 등 상태를 보존한다.</summary>
    private void SortCollection(ObservableCollection<TopicNode> collection)
    {
        var sorted = IsSortDescending
            ? collection.OrderByDescending(n => n.Name, NaturalStringComparer.Instance).ToList()
            : collection.OrderBy(n => n.Name, NaturalStringComparer.Instance).ToList();

        ApplySortedOrder(collection, sorted);
    }

    /// <summary>평점 기준으로 대주제 목록(Topics)만 정렬한다(소주제 목록엔 적용 안 함 — 평점은 대주제 전용 값).
    /// 값이 같으면 이름으로 2차 정렬한다.</summary>
    private void SortTopicsByRating()
    {
        var sorted = IsRatingSortDescending
            ? Topics.OrderByDescending(n => n.Rating).ThenBy(n => n.Name, NaturalStringComparer.Instance).ToList()
            : Topics.OrderBy(n => n.Rating).ThenBy(n => n.Name, NaturalStringComparer.Instance).ToList();

        ApplySortedOrder(Topics, sorted);
    }

    /// <summary>새 대주제 추가/이름변경/평점 변경 후, 마지막으로 눌렀던 정렬 버튼(이름 또는 평점) 기준으로 대주제
    /// 목록의 위치를 다시 맞춘다.</summary>
    private void ResortTopics()
    {
        if (_activeSortMode == TopicSortMode.Rating)
        {
            SortTopicsByRating();
        }
        else
        {
            SortCollection(Topics);
        }
    }

    private static void ApplySortedOrder(ObservableCollection<TopicNode> collection, List<TopicNode> sorted)
    {
        for (var i = 0; i < sorted.Count; i++)
        {
            var currentIndex = collection.IndexOf(sorted[i]);
            if (currentIndex != i)
            {
                collection.Move(currentIndex, i);
            }
        }
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
            ResortTopics();
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
            SortCollection(majorTopic.Children);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "소주제 추가 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>팝업 메뉴 "평점 설정" → 대화상자에서 1~10 값을 골라 대주제의 progress bar에 반영한다 (04-topic-management.md).</summary>
    [RelayCommand]
    private void SetMajorTopicRating(TopicNode? node)
    {
        if (node is not { IsMajorTopic: true })
        {
            return;
        }

        var dialog = new TopicRatingDialog(node.Name, node.Rating)
        {
            Owner = Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() == true)
        {
            node.Rating = dialog.SelectedValue;

            if (_activeSortMode == TopicSortMode.Rating)
            {
                SortTopicsByRating();
            }
        }
    }

    /// <summary>팝업 메뉴 "장르 설정" → 미리 정의된 목록(복수 선택)에서 골라 홈 화면 카드에 반영한다 (04-topic-management.md).</summary>
    [RelayCommand]
    private void SetMajorTopicGenre(TopicNode? node)
    {
        if (node is not { IsMajorTopic: true })
        {
            return;
        }

        var dialog = new TopicGenreDialog(node.Name, node.Genre)
        {
            Owner = Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() == true)
        {
            node.Genre = dialog.SelectedGenres;
        }
    }

    /// <summary>팝업 메뉴 "썸네일 설정" → 파일을 골라 thumbnails/ 폴더로 복사하고 홈 화면 카드에 반영한다
    /// (04-topic-management.md). 홈 화면 카드로 드래그 앤 드롭하는 방법도 있다(`HomeView.Card_Drop`).</summary>
    [RelayCommand]
    private void SetMajorTopicThumbnail(TopicNode? node)
    {
        if (node is not { IsMajorTopic: true })
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "썸네일 이미지 선택",
            Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff",
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ApplyThumbnail(node, dialog.FileName, deleteSource: false);
    }

    /// <summary>파일 선택 대화상자와 홈 화면 카드 드래그 앤 드롭(`HomeView.Card_Drop`)이 공유하는 저장 로직.</summary>
    public void ApplyThumbnail(TopicNode node, string sourceImagePath, bool deleteSource)
    {
        try
        {
            var newPath = ThumbnailStore.Save(sourceImagePath, node.Name, deleteSource);

            // ThumbnailStore.Save는 항상 "{대주제 이름}.original.{확장자}"에 저장하므로, 새로 지정한
            // 이미지가 이전 것과 확장자가 같으면 경로 문자열 자체는 그대로다. ObservableProperty가 생성한
            // setter는 값이 실제로 바뀔 때만 PropertyChanged를 올리므로, 그냥 대입하면 파일은 새로
            // 덮어써졌는데 카드 이미지는 갱신되지 않는다(game-platform에서 실제로 겪은 버그) — null로 한 번
            // 지웠다가 다시 지정해서 항상 알림이 나가게 한다.
            node.ThumbnailPath = null;
            node.ThumbnailPath = newPath;
        }
        catch (IOException ex)
        {
            MessageBox.Show(ex.Message, "썸네일 설정 실패", MessageBoxButton.OK, MessageBoxImage.Error);
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

        var oldName = node.Name;

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

        // 썸네일은 대주제 이름으로 파일명을 짓기 때문에(ThumbnailStore), 이름이 바뀌면 파일도 같이 옮겨야
        // 평점/장르와 달리 끊기지 않는다.
        if (node.IsMajorTopic && node.ThumbnailPath is { } thumbnailPath)
        {
            var renamedPath = ThumbnailStore.Rename(thumbnailPath, oldName, node.Name);
            if (renamedPath is not null)
            {
                node.ThumbnailPath = renamedPath;
            }
        }

        // 이름이 바뀌었으니 정렬 순서상 위치도 다시 맞춘다.
        if (node.IsMajorTopic)
        {
            ResortTopics();
        }
        else
        {
            var parent = Topics.FirstOrDefault(t => t.Children.Contains(node));
            if (parent is not null)
            {
                SortCollection(parent.Children);
            }
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

    /// <summary>
    /// 세션 복원용: 이름으로 대주제(minorName이 null이면 대주제 자체) 또는 소주제를 찾아 선택 상태로 만든다.
    /// 대주제/소주제가 더 이상 존재하지 않으면 조용히 아무것도 선택하지 않는다 (02-architecture.md 복원 예외 처리).
    /// </summary>
    public void SelectByName(string? majorName, string? minorName)
    {
        if (majorName is null)
        {
            return;
        }

        var major = Topics.FirstOrDefault(t => t.Name == majorName);
        if (major is null)
        {
            return;
        }

        major.IsExpanded = true;

        if (minorName is null)
        {
            major.IsSelected = true;
            SelectedNode = major;
            return;
        }

        var minor = major.Children.FirstOrDefault(c => c.Name == minorName);
        if (minor is null)
        {
            return;
        }

        minor.IsSelected = true;
        SelectedNode = minor;
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
