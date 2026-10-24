# 02. 아키텍처

## 패턴
MVVM (Model-View-ViewModel) 권장. `CommunityToolkit.Mvvm` NuGet 패키지 사용 검토.

## 제안 프로젝트 구조
```
ImageTopicViewer/
├── ImageTopicViewer.sln
├── src/
│   └── ImageTopicViewer/
│       ├── App.xaml
│       ├── Views/
│       │   ├── MainWindow.xaml
│       │   ├── TopicTreeView.xaml
│       │   ├── ContinuousPageView.xaml
│       │   ├── SingleImageView.xaml
│       │   └── DataFolderPickerDialog.xaml   // 최초 실행 시 데이터 폴더 지정
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── TopicTreeViewModel.cs
│       │   └── ImagePageViewModel.cs
│       ├── Models/
│       │   ├── TopicNode.cs        // 대주제/소주제 노드 (IsArchive: 압축 파일 기반 소주제 여부)
│       │   ├── ImageItem.cs        // 개별 이미지 항목 (폴더 파일 또는 압축 파일 내부 항목)
│       │   └── AppSettings.cs      // 데이터 폴더 경로, 창 상태, 마지막 세션 상태
│       ├── Services/
│       │   ├── ITopicRepository.cs      // 대주제/소주제 CRUD (폴더 스캔 기반)
│       │   ├── IImageStorageService.cs  // 이미지 저장/이동/재넘버링/포맷 변환
│       │   ├── IImageSourceProvider.cs  // 뷰에 표시할 이미지 소스 제공 (비동기 로딩, 추후 캐싱 확장 지점)
│       │   ├── ISettingsService.cs      // 데이터 폴더 경로 읽기/쓰기 (03 참조)
│       │   ├── IManagementService.cs    // 소주제 업데이트 날짜 등 관리 데이터 읽기/쓰기 (management.json)
│       │   └── FileSystemTopicRepository.cs
│       └── Converters/
└── doc/
```

> 실제 이미지가 저장되는 데이터 폴더(`.data`)는 프로젝트/실행 폴더에 고정되지 않고 **사용자가 지정**합니다. 위치 결정 방식은 아래 "데이터 폴더 위치" 절과 `03-data-storage.md` 참조.

## 데이터 폴더 위치
- 앱을 **최초 실행할 때 폴더 선택 다이얼로그를 필수로 표시**하여 사용자가 데이터 폴더를 지정한다.
- 지정한 경로는 `%APPDATA%\ImageTopicViewer\settings.json`에 저장한다(`ISettingsService` 담당).
- 이후 실행부터는 저장된 경로를 자동으로 읽어 사용하며, 폴더 선택 다이얼로그를 다시 띄우지 않는다.
- 설정 메뉴에서 데이터 폴더를 변경할 수 있다. **변경 시 기존 데이터는 새 위치로 이동하지 않으며, 새 폴더는 빈 상태로 시작한다.**

## 세션 상태 저장/복원
앱 종료 시 다음 상태를 `%APPDATA%\ImageTopicViewer\settings.json`(데이터 폴더 경로와 같은 설정 파일)에 저장하고, 다음 실행 시 복원한다.
- **창 위치/크기**: `Left`, `Top`, `Width`, `Height`, 최대화 여부(`WindowState`).
- **마지막 선택 주제**: 마지막으로 선택했던 대주제/소주제. 소주제를 선택했으면 대주제+소주제 이름을, 대주제만 선택했으면 대주제 이름만 저장한다(2026-09-21 — 이전엔 대주제만 선택한 경우 저장되지 않았음). 복원 시 해당 노드를 선택하고 좌측 트리를 그 위치로 스크롤한다(`07-ui-layout.md` "좌측 패널").
- **마지막 뷰 모드**: 연속보기/단일보기 중 마지막으로 사용하던 모드. (스크롤 위치 복원이 의미를 가지려면 뷰 모드도 함께 복원되어야 하므로 포함)
- **스크롤/위치 상태**: 연속보기였다면 마지막 스크롤 오프셋, 단일보기였다면 마지막으로 보던 이미지 인덱스.
- **확대/축소 배율**: 마지막 선택한 % 값(`06-view-modes.md`).
- **스크롤 배속**: 연속보기 마우스 휠 배속(x1~x10) 마지막 선택값(2026-09-22 추가, `06-view-modes.md` "연속보기").
- **소주제별 읽던 위치**: `"{대주제}/{소주제}"`를 키로 하는 딕셔너리(`TopicProgress`)에 각 소주제에서 마지막으로 보던 이미지 인덱스와 그때의 전체 이미지 수를 기록한다. 세션 전체에서 열어본 모든 소주제가 대상이며, 트리에서 이름 옆 표시에 쓰인다 (`07-ui-layout.md` "좌측 패널" 참조).
- **대주제별 평점/장르/썸네일 경로/최근 본 소주제**: 대주제 이름을 키로 하는 딕셔너리 4개(`MajorTopicRating`, `MajorTopicGenre`, `MajorTopicThumbnailPath`, `MajorTopicLastMinorTopic` — 2026-10-18 장르/썸네일, 2026-10-19 최근 본 소주제 추가). 홈 화면 카드와 좌측 트리 progress bar에 쓰인다 (`04-topic-management.md` "대주제 평점", "대주제 장르/썸네일", `07-ui-layout.md` "홈 화면"). 넷 다 대주제 이름이 바뀌면 키가 달라져 끊기는 게 원칙이지만, 썸네일 경로만은 예외로 `TopicTreeViewModel.RenameTopic`이 파일까지 함께 옮겨 끊기지 않는다.
  - `MajorTopicLastMinorTopic`은 소주제를 선택할 때마다(`MainViewModel.RememberLastViewedMinorTopic`) 그 부모 대주제 값이 갱신된다. 홈 화면 카드를 클릭하면 이 값으로 지정된 소주제가 있는 경우 대주제만 펼치는 대신 그 소주제로 바로 이동한다. 이 필드가 생기기 전에 열어본 소주제는 기록이 없어 처음엔 대주제만 펼쳐지고, 그 다음부터 열람할 때 기록이 새로 쌓인다.

**홈 화면 전환 상태는 세션 간 저장되지 않는다**: `MainViewModel.IsHomeScreen`은 앱을 실행할 때마다 항상 `true`로 시작한다(위 "마지막 선택 주제" 복원과는 별개로, 첫 화면은 항상 홈이다). 카드를 클릭하면 `false`로 바뀌어 기존 화면이 드러나고, 툴바의 "홈" 버튼으로 언제든 다시 `true`로 되돌릴 수 있다.

복원 시 예외 처리:
- 창 위치가 현재 연결된 모니터 구성에서 화면 밖으로 벗어나면(예: 모니터 연결 해제) 기본 위치/크기로 대체한다.
- 마지막 선택 주제가 더 이상 존재하지 않으면(그 사이 삭제/이름변경됨) 주제 미선택 상태로 시작한다("좌측에서 주제를 선택하세요").
- `06-view-modes.md`의 "스크롤 위치는 소주제 전환 시 초기화"는 같은 세션 내에서 다른 소주제로 전환할 때만 적용되며, 앱 재시작 시의 복원과는 별개다.

## 핵심 서비스 책임
- **ISettingsService**: 데이터 폴더 경로, 창 상태, 마지막 세션 상태(선택 주제/뷰 모드/스크롤 위치)를 포함한 앱 설정을 `%APPDATA%\ImageTopicViewer\settings.json`에서 읽고 쓴다. 최초 실행 여부 판단(설정 파일 부재 시 폴더 선택 다이얼로그 트리거)도 담당. 창 상태와 세션 상태는 앱 종료 시(`MainWindow.Closing`) 저장한다.
- **IManagementService**(2026-10-24 추가): `ISettingsService`와 같은 패턴으로 `%APPDATA%\ImageTopicViewer\management.json`을 읽고 쓰지만, 내용은 settings.json과 분리된 "관리" 데이터뿐이다 — 현재는 소주제별 업데이트 날짜/시간(`TopicManagementData.MinorTopicUpdatedAt`, "{대주제}/{소주제}" 키, `04-topic-management.md` "관리"). 앱 종료를 기다리지 않고 값이 바뀔 때마다(`MainViewModel.OnMinorTopicContentChanged`) 즉시 저장한다.
- **ITopicRepository**: 설정에 저장된 데이터 폴더를 스캔하여 대주제/소주제 트리를 구성. 주제 추가/삭제(휴지통 이동)/이름변경(폴더 rename + 내부 파일 재넘버링 트리거) 담당.
- **IImageStorageService**: 이미지 파일 이동, PNG 포맷 통일 변환, 파일명 규칙 적용, 순서 변경/삭제(휴지통 이동) 시 재넘버링 로직 담당. (상세는 `03-data-storage.md` 참조)
  - 삭제는 영구 삭제가 아니라 **Windows 휴지통으로 이동**한다 (`Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile`/`DeleteDirectory`의 `RecycleOption.SendToRecycleBin` 사용 검토).
- **IImageSourceProvider**: 연속보기/단일보기가 화면에 그릴 이미지 소스를 비동기로 제공. v1은 원본 파일(또는 압축 파일 내부 항목)을 그대로 비동기 로드하는 단순 구현이지만, 이 인터페이스 뒤에서 동작하므로 나중에 성능 이슈가 생기면 뷰 코드 변경 없이 썸네일 캐싱 구현으로 교체할 수 있다. `LoadAsync(ImageItem item, ...)`는 `item.IsFromArchive` 여부로 폴더 파일 로드와 zip 엔트리 로드를 분기한다. (`03-data-storage.md`의 "압축 파일 기반 소주제", `06-view-modes.md`, `08-open-decisions.md` 참조)
- **ThumbnailStore**(정적 클래스, 2026-10-18 추가 — 2026-10-19 "커버 이미지"에서 "썸네일" 용어로 개명, 저장 방식을 game-platform 프로젝트의 `ThumbnailHelper`에서 그대로 가져옴): 홈 화면 카드의 썸네일을 사용자가 고르면(`07-ui-layout.md` "홈 화면") 리사이즈 없이 원본 그대로 `E:\code\image-readers\thumbnails\{대주제 이름}.original{확장자}`로 복사해 보관한다(2026-10-19 사용자 요청으로 `%APPDATA%\ImageTopicViewer\thumbnails\`에서 이 저장소 폴더 바로 밑 고정 경로로 변경 — `.gitignore` 처리되어 커밋 대상은 아니다). 대주제 이름이 이미 `TopicNameValidator`를 통과한 값이라 파일명으로 그대로 쓸 수 있다(GUID 대신 이름 기반). 다른 확장자로 교체하면 이전 확장자 파일을 지우고, `Rename`으로 대주제 이름 변경 시 파일도 같이 옮긴다. `ITopicRepository`를 거치지 않는 이유는 이미지가 데이터 폴더(대주제/소주제 폴더) 바깥, 앱 설정 영역에 속하기 때문이다.
- **DragDropImageHelper**(정적 클래스, 2026-10-19 추가, game-platform 프로젝트에서 그대로 가져옴): 드래그 앤 드롭된 데이터에서 이미지를 꺼내 파일 경로로 확보한다. 로컬 파일 드롭은 그 경로를 그대로, 웹 브라우저에서 드래그한 이미지(HTTP(S) URL, `data:` URI, HTML `img`/`srcset`, 렌더링된 비트맵)는 임시 파일로 받아 경로를 반환한다 — 후자는 `isTemporary=true`로 표시되어 `ThumbnailStore.Save`가 복사 후 삭제한다. 홈 화면 카드에 이미지를 직접 드래그해 썸네일을 지정하는 데 쓴다(`07-ui-layout.md` "홈 화면", `TopicTreeViewModel.ApplyThumbnail`).
  - game-platform 원본에는 없던 처리를 하나 추가했다(2026-10-19, Edge 드래그가 안 된다는 리포트로 발견): Edge/Chrome은 웹 이미지를 드래그할 때 위 URL 텍스트 형식 없이 "가상 파일"(`FileGroupDescriptor`/`FileContents`)만 주는 경우가 많다. 이 형식은 이 앱이 이미지 추가 드래그드롭(`05-image-features.md`, `Views/VirtualFileDropReader.cs`)에 이미 쓰고 있던 것이라, 그 리더를 `DragDropImageHelper`에도 연결해 로컬 파일 다음·URL 추출보다 먼저 시도하게 했다(브라우저가 이미 받아온 픽셀이라 자체 HTTP 재다운로드보다 핫링크 차단/인증 문제에 덜 걸린다).
- **ImageLoadHelper**(정적 클래스, 2026-10-19 추가, game-platform 프로젝트에서 그대로 가져옴): 로컬 파일 경로에서 `BitmapImage`를 `BitmapCacheOption.OnLoad` + `BitmapCreateOptions.IgnoreImageCache`로 즉시 전부 읽어(`Freeze`) 반환한다. 썸네일을 교체하면 같은 경로(`{대주제 이름}.original.*`)를 덮어쓰므로, 이 두 옵션이 없으면 WPF가 파일 핸들을 계속 잡고 있어 덮어쓰기가 실패하거나(핸들 충돌) 덮어쓴 뒤에도 예전 픽셀을 그대로 보여준다(URI 기준 캐시). `decodePixelWidth`로 디코딩 폭을 제한할 수 있다(`ThumbnailBrushConverter`가 카드 폭의 2배를 넘겨 Ctrl+스크롤 확대에도 선명하게 보이도록 한다).
- **EdgeZoomLevels**(정적 클래스, 2026-10-24 추가): Microsoft Edge/Chrome의 확대/축소 단계 목록(25~500%, `06-view-modes.md` "확대/축소")과 `Step(현재값, 방향)` 헬퍼를 담는다. `ContinuousPageViewModel`(연속보기/단일보기 이미지)과 `MainViewModel`(홈 화면 카드, `07-ui-layout.md` "홈 화면")이 같은 목록/로직을 공유해 중복을 없앴다. 두 ViewModel 모두 값을 바꿀 때마다 `DispatcherTimer`로 제어하는 "확대/축소 팝업"(`IsZoomIndicatorVisible`/`IsHomeZoomIndicatorVisible`)을 잠깐 띄운다 — Microsoft Edge의 확대/축소 토스트와 같은 동작.

## 데이터 소스에 대한 설계 결정
별도 DB/JSON 메타데이터 파일 없이, **폴더 구조 + 파일명 자체를 데이터 소스로 사용**한다.
- 장점: 단순함, 파일탐색기로도 내용 확인 가능
- 제약: 대주제/소주제 이름에 Windows 폴더명 금지 문자(`\ / : * ? " < > |`) 사용 불가 → 입력 검증 필요
