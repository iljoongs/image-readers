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
│       │   └── AppSettings.cs      // 데이터 폴더 경로 등 앱 설정
│       ├── Services/
│       │   ├── ITopicRepository.cs      // 대주제/소주제 CRUD (폴더 스캔 기반)
│       │   ├── IImageStorageService.cs  // 이미지 저장/이동/재넘버링
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

## 핵심 서비스 책임
- **ISettingsService**: 데이터 폴더 경로를 포함한 앱 설정을 `%APPDATA%\ImageTopicViewer\settings.json`에서 읽고 쓴다. 최초 실행 여부 판단(설정 파일 부재 시 폴더 선택 다이얼로그 트리거)도 담당.
- **ITopicRepository**: 설정에 저장된 데이터 폴더를 스캔하여 대주제/소주제 트리를 구성. 주제 추가/삭제/이름변경(폴더 rename + 내부 파일 재넘버링 트리거) 담당.
- **IImageStorageService**: 이미지 파일 이동, 파일명 규칙 적용, 순서 변경 시 재넘버링 로직 담당. (상세는 `03-data-storage.md` 참조)

## 데이터 소스에 대한 설계 결정
별도 DB/JSON 메타데이터 파일 없이, **폴더 구조 + 파일명 자체를 데이터 소스로 사용**한다.
- 장점: 단순함, 파일탐색기로도 내용 확인 가능
- 제약: 대주제/소주제 이름에 Windows 폴더명 금지 문자(`\ / : * ? " < > |`) 사용 불가 → 입력 검증 필요
