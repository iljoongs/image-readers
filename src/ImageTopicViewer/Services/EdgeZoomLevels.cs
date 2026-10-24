namespace ImageTopicViewer.Services;

/// <summary>Microsoft Edge/Chrome이 쓰는 확대/축소 단계 목록(%, 25~500). 연속보기/단일보기 이미지 확대축소
/// (`ContinuousPageViewModel`)와 홈 화면 카드 확대축소(`MainViewModel`)가 공유한다 (06-view-modes.md "확대/축소",
/// 07-ui-layout.md "홈 화면").</summary>
public static class EdgeZoomLevels
{
    public static readonly IReadOnlyList<double> Values = new double[]
    {
        25, 33, 50, 67, 75, 80, 90, 100, 110, 125, 150, 175, 200, 250, 300, 400, 500,
    };

    /// <summary>현재 값에서 단계 목록을 따라 한 칸 위(direction=1)/아래(direction=-1)로 이동한 값을 반환한다.
    /// 목록에 정확히 없는 값(예: 이전 버전에서 복원된 값)이 들어오면 그보다 크거나 같은 가장 가까운 단계로
    /// 스냅한 뒤 이동한다.</summary>
    public static double Step(double currentPercent, int direction)
    {
        var currentIndex = IndexOf(currentPercent);
        var newIndex = Math.Clamp(currentIndex + direction, 0, Values.Count - 1);
        return Values[newIndex];
    }

    private static int IndexOf(double percent)
    {
        for (var i = 0; i < Values.Count; i++)
        {
            if (Math.Abs(Values[i] - percent) < 0.01)
            {
                return i;
            }
        }

        for (var i = 0; i < Values.Count; i++)
        {
            if (Values[i] >= percent)
            {
                return i;
            }
        }

        return Values.Count - 1;
    }
}
