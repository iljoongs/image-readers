# 09. 재사용 가능한 패턴: Microsoft Edge 스타일 확대/축소

이 문서는 이 프로젝트(ImageTopicViewer)에 구현된 "Edge 스타일 확대/축소"를 **다른 WPF 프로젝트에도 그대로
옮겨 쓸 수 있도록** 일반화해서 정리한 것이다. `06-view-modes.md`(연속보기/단일보기 이미지 확대축소)와
`07-ui-layout.md`(홈 화면 카드 확대축소) 두 곳에 실제로 적용되어 있으니, 구체적인 동작은 그쪽을 보고
이 문서는 "어떻게 다른 프로젝트에 이식하느냐"에 집중한다.

## 무엇을 재사용하는가

세 가지 독립된 조각으로 나뉘며, 각각 단독으로도 쓸 수 있다.

1. **공유 배율 단계 목록 + 이동 로직** (`EdgeZoomLevels`) — 10%p 단위 같은 연속값이 아니라, 브라우저처럼
   불연속 단계(25/33/50/67/75/80/90/100/110/125/150/175/200/250/300/400/500%)를 오르내리는 방식.
2. **Ctrl+휠로 조절하는 ViewModel 패턴** — 배율 값 + "지금 막 조작했다"는 신호(팝업 표시 여부)를 한 세트로
   관리하고, 일정 시간 조작이 없으면 자동으로 신호를 끄는 타이머.
3. **상단에 잠깐 떴다 사라지는 배율 팝업(토스트) XAML** — `[-] {배율}% [+]` 모양의 반투명 알약, 겹쳐 그리기.

## 1. 공유 배율 단계 목록 — `EdgeZoomLevels`

그대로 복사해서 쓸 수 있다(프로젝트 고유 의존성 없음 — `System.Collections.Generic`/`System.Math`뿐):

```csharp
public static class EdgeZoomLevels
{
    public static readonly IReadOnlyList<double> Values = new double[]
    {
        25, 33, 50, 67, 75, 80, 90, 100, 110, 125, 150, 175, 200, 250, 300, 400, 500,
    };

    /// <summary>현재 값에서 목록을 따라 한 칸 위(direction=1)/아래(direction=-1)로 이동한 값을 반환한다.
    /// 목록에 정확히 없는 값(예: 다른 방식에서 넘어온 값)이 들어오면 그보다 크거나 같은 가장 가까운 단계로
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
            if (Math.Abs(Values[i] - percent) < 0.01) return i;
        }
        for (var i = 0; i < Values.Count; i++)
        {
            if (Values[i] >= percent) return i;
        }
        return Values.Count - 1;
    }
}
```

**왜 콤보박스/슬라이더가 아니라 이 방식인가**: Microsoft Edge/Chrome은 확대/축소 배율을 자유 입력이나
10%p 등간격이 아니라 저 17단계로만 오르내린다. 사람이 "한 칸 더/덜"로 조작하는 느낌을 흉내 내려면, 현재
값이 목록의 몇 번째인지 찾고 ±1칸 이동하는 이 `Step` 함수 하나면 충분하다 — 현재 값 보관은 호출하는 쪽
(ViewModel)의 배율 속성이 맡는다.

**여러 화면이 같은 배율 목록을 쓸 때**: 이 프로젝트에서는 이미지 뷰어와 홈 화면 카드가 서로 다른
ViewModel에 각자 배율 속성을 갖고 있지만(`ContinuousPageViewModel.ZoomPercent`,
`MainViewModel.HomeZoomPercent`), 단계 목록과 이동 로직은 `EdgeZoomLevels` 하나를 공유한다. 다른
프로젝트에 여러 확대/축소 대상이 있다면 같은 방식으로 로직만 공유하고 "현재 값"은 각자 갖게 하는 편이
깔끔하다.

## 2. ViewModel 패턴 — 배율 + 팝업 표시 + 자동 숨김 타이머

CommunityToolkit.Mvvm의 `ObservableObject`/`[ObservableProperty]`/`[RelayCommand]`를 쓴다는 전제다(다른
MVVM 프레임워크를 쓴다면 속성 변경 알림 부분만 그에 맞게 바꾸면 된다).

```csharp
using System.Windows.Threading; // DispatcherTimer

public partial class SomeViewModel : ObservableObject
{
    private static readonly TimeSpan ZoomIndicatorHideDelay = TimeSpan.FromSeconds(1.5);

    [ObservableProperty]
    private double _zoomPercent = 100;

    /// <summary>팝업(토스트) 표시 여부. View가 이 값에 Visibility를 바인딩한다.</summary>
    [ObservableProperty]
    private bool _isZoomIndicatorVisible;

    private readonly DispatcherTimer _zoomIndicatorTimer;

    public SomeViewModel()
    {
        _zoomIndicatorTimer = new DispatcherTimer { Interval = ZoomIndicatorHideDelay };
        _zoomIndicatorTimer.Tick += (_, _) =>
        {
            _zoomIndicatorTimer.Stop();
            IsZoomIndicatorVisible = false;
        };
    }

    [RelayCommand]
    private void ZoomIn() => AdjustZoom(1);

    [RelayCommand]
    private void ZoomOut() => AdjustZoom(-1);

    private void AdjustZoom(int direction)
    {
        ZoomPercent = EdgeZoomLevels.Step(ZoomPercent, direction);

        IsZoomIndicatorVisible = true;
        _zoomIndicatorTimer.Stop();   // 조작할 때마다 타이머를 리셋 — 쉬지 않고 돌리면 팝업이 계속 떠 있다.
        _zoomIndicatorTimer.Start();
    }
}
```

**핵심은 "타이머를 매번 멈췄다가 다시 시작"하는 것**이다. `Start()`만 반복 호출하면 이미 돌고 있는
타이머는 그대로 두고 새 타이머가 또 쌓이는 게 아니라 "다음 Tick까지 남은 시간"이 리셋되지 않는 구현도
있으므로(프레임워크에 따라 다름), WPF `DispatcherTimer`는 `Stop()` 후 `Start()`로 확실하게 리셋한다.

## 3. View 패턴 — Ctrl+휠 가로채기 + 팝업 XAML

### Ctrl+휠 가로채기 (code-behind)

```csharp
private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
{
    if (DataContext is not SomeViewModel viewModel || Keyboard.Modifiers != ModifierKeys.Control)
    {
        return; // Ctrl이 아니면 가로채지 않는다 — 평소 스크롤은 그대로 동작해야 한다.
    }

    if (e.Delta > 0) viewModel.ZoomInCommand.Execute(null);
    else viewModel.ZoomOutCommand.Execute(null);

    e.Handled = true; // 평소 스크롤(페이지 넘김 등)로 번지지 않도록 막는다.
}
```

`PreviewMouseWheel`(터널링, 즉 부모→자식 순서로 먼저 도착)에 연결해야 자식 컨트롤(`ScrollViewer` 등)이
먼저 자기 스크롤로 소비해버리기 전에 가로챌 수 있다. 여러 뷰(예: 연속보기/단일보기)가 같은 ViewModel을
공유한다면, 각 뷰의 루트에 똑같이 걸어주면 된다 — 배율 상태는 ViewModel에 있으므로 뷰를 전환해도
끊기지 않는다.

### 팝업(토스트) XAML

```xml
<!-- 겹쳐 그릴 영역(예: 메인 콘텐츠를 담은 Grid)의 자식으로, 다른 콘텐츠보다 뒤에(= 나중에) 선언한다. -->
<Border HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,16,0,0"
        Background="#CC202020" CornerRadius="18" Padding="6,2"
        Visibility="{Binding IsZoomIndicatorVisible, Converter={StaticResource BoolToVisibilityConverter}}">
    <StackPanel Orientation="Horizontal">
        <Button Content="-" Width="30" Height="30" Padding="0" FontSize="16" FontWeight="Bold"
                Background="Transparent" Foreground="White" BorderThickness="0"
                Command="{Binding ZoomOutCommand}"/>
        <TextBlock Text="{Binding ZoomPercent, StringFormat={}{0:0}%}"
                   Foreground="White" FontSize="13" Width="48" TextAlignment="Center"
                   VerticalAlignment="Center"/>
        <Button Content="+" Width="30" Height="30" Padding="0" FontSize="16" FontWeight="Bold"
                Background="Transparent" Foreground="White" BorderThickness="0"
                Command="{Binding ZoomInCommand}"/>
    </StackPanel>
</Border>
```

필요한 것:
- `BoolToVisibilityConverter`(`bool`→`Visibility`, `true`=`Visible`/`false`=`Collapsed`) 하나. WPF 기본
  제공은 아니므로 프로젝트에 없으면 간단히 만들어 등록해야 한다(이 프로젝트의
  `Converters/BoolToVisibilityConverter.cs` 참고).
- 겹쳐 그릴 부모가 **여러 화면(View)을 Grid 한 칸에 공유**하는 경우(이 프로젝트의 연속보기/단일보기처럼),
  팝업은 그 부모 Grid에 **한 번만** 선언하면 된다 — 자식 뷰가 바뀌어도 팝업은 계속 같은 자리에 떠서
  어느 쪽이 보이든 동일하게 동작한다.
- `StringFormat={}{0:0}%`에서 앞의 `{}`는 WPF가 `{0:0}%`를 마크업 확장으로 오인하지 않게 하는
  이스케이프 — 빠뜨리면 XAML 파싱 오류가 난다.

## 다른 프로젝트에 적용하는 순서 (체크리스트)

1. `EdgeZoomLevels.cs`를 그대로 복사해 넣는다(네임스페이스만 바꾸면 끝).
2. 확대/축소 대상이 있는 ViewModel에 `ZoomPercent`(배율) + `IsZoomIndicatorVisible`(팝업 표시) +
   `DispatcherTimer` + `ZoomIn`/`ZoomOut` 커맨드를 추가한다(2번 섹션 그대로).
3. 그 ViewModel을 쓰는 View(들)의 루트 요소에 `PreviewMouseWheel` 핸들러를 건다(3번 섹션 "Ctrl+휠
   가로채기" 그대로, 여러 View가 같은 ViewModel을 공유하면 각 View에 똑같이).
4. 팝업이 보일 자리(보통 메인 콘텐츠를 담은 `Grid`의 가장 마지막 자식)에 토스트 XAML을 넣는다.
5. 기존에 "배율 선택 콤보박스/슬라이더" 같은 게 있었다면, Edge도 그런 걸 상시 노출하지 않으므로 함께
   정리할지 사용자에게 확인한다(이 프로젝트에서는 제거했다 — `06-view-modes.md` "확대/축소" 2026-10-24
   항목 참조).
6. 이미 배율을 설정 파일 등에 저장/복원하던 기존 로직이 있다면 그대로 둬도 된다 — 저장된 값이 새
   단계 목록에 정확히 없어도 `EdgeZoomLevels.Step`이 다음 조작 때 가장 가까운 단계로 스냅해 준다
   (크래시 없음).

## 이 프로젝트에서의 실제 적용 위치 (참고용)

- `src/ImageTopicViewer/Services/EdgeZoomLevels.cs` — 공유 목록/로직.
- `src/ImageTopicViewer/ViewModels/ContinuousPageViewModel.cs` — 이미지 뷰어 배율(`ZoomPercent`) +
  `IsZoomIndicatorVisible` + 타이머.
- `src/ImageTopicViewer/ViewModels/MainViewModel.cs` — 홈 화면 카드 배율(`HomeZoomPercent`) +
  `IsHomeZoomIndicatorVisible` + 타이머(동일 패턴, 두 번째 적용 사례).
- `src/ImageTopicViewer/Views/ContinuousPageView.xaml.cs`, `SingleImageView.xaml.cs` — `PreviewMouseWheel`
  가로채기 (두 뷰가 `ContinuousPageViewModel`을 공유하므로 각자 걸어둠).
- `src/ImageTopicViewer/Views/HomeView.xaml.cs` — 홈 화면용 `PreviewMouseWheel` 가로채기.
- `src/ImageTopicViewer/Views/MainWindow.xaml` — 이미지 뷰어용 팝업(메인 영역 `Grid`의 마지막 자식).
- `src/ImageTopicViewer/Views/HomeView.xaml` — 홈 화면용 팝업.
