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
│       │   ├── TopicNode.cs        // 대주제/소주제 노드
│       │   ├── ImageItem.cs        // 개별 이미지 항목
│       │   └── AppSettings.cs      // 데이터 폴더 경로, 창 상태, 마지막 세션 상태
│       ├── Services/
│       │   ├── ITopicRepository.cs      // 대주제/소주제 CRUD (폴더 스캔 기반)
│       │   ├── IImageStorageService.cs  // 이미지 저장/이동/재넘버링/포맷 변환
│       │   ├── IImageSourceProvider.cs  // 뷰에 표시할 이미지 소스 제공 (비동기 로딩, 추후 캐싱 확장 지점)
│       │   ├── ISettingsService.cs      // 데이터 폴더 경로 읽기/쓰기 (03 참조)
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
- **마지막 선택 주제**: 마지막으로 선택했던 대주제/소주제.
- **마지막 뷰 모드**: 연속보기/단일보기 중 마지막으로 사용하던 모드. (스크롤 위치 복원이 의미를 가지려면 뷰 모드도 함께 복원되어야 하므로 포함)
- **스크롤/위치 상태**: 연속보기였다면 마지막 스크롤 오프셋, 단일보기였다면 마지막으로 보던 이미지 인덱스.

복원 시 예외 처리:
- 창 위치가 현재 연결된 모니터 구성에서 화면 밖으로 벗어나면(예: 모니터 연결 해제) 기본 위치/크기로 대체한다.
- 마지막 선택 주제가 더 이상 존재하지 않으면(그 사이 삭제/이름변경됨) 주제 미선택 상태로 시작한다("좌측에서 주제를 선택하세요").
- `06-view-modes.md`의 "스크롤 위치는 소주제 전환 시 초기화"는 같은 세션 내에서 다른 소주제로 전환할 때만 적용되며, 앱 재시작 시의 복원과는 별개다.

## 핵심 서비스 책임
- **ISettingsService**: 데이터 폴더 경로, 창 상태, 마지막 세션 상태(선택 주제/뷰 모드/스크롤 위치)를 포함한 앱 설정을 `%APPDATA%\ImageTopicViewer\settings.json`에서 읽고 쓴다. 최초 실행 여부 판단(설정 파일 부재 시 폴더 선택 다이얼로그 트리거)도 담당. 창 상태와 세션 상태는 앱 종료 시(`MainWindow.Closing`) 저장한다.
- **ITopicRepository**: 설정에 저장된 데이터 폴더를 스캔하여 대주제/소주제 트리를 구성. 주제 추가/삭제(휴지통 이동)/이름변경(폴더 rename + 내부 파일 재넘버링 트리거) 담당.
- **IImageStorageService**: 이미지 파일 이동, PNG 포맷 통일 변환, 파일명 규칙 적용, 순서 변경/삭제(휴지통 이동) 시 재넘버링 로직 담당. (상세는 `03-data-storage.md` 참조)
  - 삭제는 영구 삭제가 아니라 **Windows 휴지통으로 이동**한다 (`Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile`/`DeleteDirectory`의 `RecycleOption.SendToRecycleBin` 사용 검토).
- **IImageSourceProvider**: 연속보기/단일보기가 화면에 그릴 이미지 소스를 비동기로 제공. v1은 원본 PNG 파일을 그대로 비동기 로드하는 단순 구현이지만, 이 인터페이스 뒤에서 동작하므로 나중에 성능 이슈가 생기면 뷰 코드 변경 없이 썸네일 캐싱 구현으로 교체할 수 있다. (`06-view-modes.md`, `08-open-decisions.md` 참조)

## 데이터 소스에 대한 설계 결정
별도 DB/JSON 메타데이터 파일 없이, **폴더 구조 + 파일명 자체를 데이터 소스로 사용**한다.
- 장점: 단순함, 파일탐색기로도 내용 확인 가능
- 제약: 대주제/소주제 이름에 Windows 폴더명 금지 문자(`\ / : * ? " < > |`) 사용 불가 → 입력 검증 필요
