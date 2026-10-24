namespace ImageTopicViewer.ViewModels;

/// <summary>상단 메뉴 "정렬"에서 고를 수 있는 정렬 기준. Windows 파일 탐색기의 "정렬 기준" 메뉴처럼 기준(이 값)과
/// 방향(TopicTreeViewModel.IsSortDescending)을 따로 고른다 (07-ui-layout.md "상단 메뉴").</summary>
public enum TopicSortKey
{
    Name,
    Rating,
}
