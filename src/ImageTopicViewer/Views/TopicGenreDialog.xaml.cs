using System.Windows;

namespace ImageTopicViewer.Views;

public partial class TopicGenreDialog : Window
{
    /// <summary>sample.png 홈 화면 상단 탭에서 그대로 따온 목록("전체" 제외) (07-ui-layout.md "홈 화면").</summary>
    public static readonly string[] PredefinedGenres =
    {
        "드라마", "판타지", "액션", "로맨스", "일상", "개그", "미스터리",
        "순정", "스포츠", "스릴러", "무협", "학원", "공포", "스토리",
    };

    public string SelectedGenres { get; private set; } = string.Empty;

    private sealed class GenreOption
    {
        public string Name { get; init; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public TopicGenreDialog(string topicName, string? currentGenre)
    {
        InitializeComponent();
        Title = "장르 설정";
        MessageText.Text = $"'{topicName}'의 장르를 선택하세요(복수 선택 가능).";

        var selected = (currentGenre ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet();

        GenreItemsControl.ItemsSource = PredefinedGenres
            .Select(name => new GenreOption { Name = name, IsSelected = selected.Contains(name) })
            .ToList();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var options = (IEnumerable<GenreOption>)GenreItemsControl.ItemsSource;
        SelectedGenres = string.Join("/", options.Where(o => o.IsSelected).Select(o => o.Name));
        DialogResult = true;
    }
}
