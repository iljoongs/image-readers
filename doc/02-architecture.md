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
│       │   └── SingleImageView.xaml
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── TopicTreeViewModel.cs
│       │   └── ImagePageViewModel.cs
│       ├── Models/
│       │   ├── TopicNode.cs        // 대주제/소주제 노드
│       │   └── ImageItem.cs        // 개별 이미지 항목
│       ├── Services/
│       │   ├── ITopicRepository.cs      // 대주제/소주제 CRUD (폴더 스캔 기반)
│       │   ├── IImageStorageService.cs  // 이미지 저장/이동/재넘버링
│       │   └── FileSystemTopicRepository.cs
│       └── Converters/
├── .data/                # 실제 이미지 저장 위치 (실행 폴더 기준)
└── doc/
```

## 핵심 서비스 책임
- **ITopicRepository**: `.data` 폴더를 스캔하여 대주제/소주제 트리를 구성. 주제 추가/삭제/이름변경(폴더 rename + 내부 파일 재넘버링 트리거) 담당.
- **IImageStorageService**: 이미지 파일 이동, 파일명 규칙 적용, 순서 변경 시 재넘버링 로직 담당. (상세는 `03-data-storage.md` 참조)

## 데이터 소스에 대한 설계 결정
별도 DB/JSON 메타데이터 파일 없이, **폴더 구조 + 파일명 자체를 데이터 소스로 사용**한다.
- 장점: 단순함, 파일탐색기로도 내용 확인 가능
- 제약: 대주제/소주제 이름에 Windows 폴더명 금지 문자(`\ / : * ? " < > |`) 사용 불가 → 입력 검증 필요
