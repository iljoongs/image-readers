# ImageTopicViewer - 메인 지시서

이 문서는 프로젝트의 메인 지시서입니다. 작업 시작 전 아래 보조 지시서를 모두 읽고 진행하세요.

## 프로젝트 한 줄 요약
주제(대주제/소주제)별로 이미지를 관리하고, 연속/단일 보기로 열람할 수 있는 WPF(.NET 8) 데스크톱 도구.

## 기술 스택
- .NET 8
- WPF (Windows Presentation Foundation)
- MVVM 패턴 권장 (CommunityToolkit.Mvvm 사용 검토)

## 보조 지시서 목록 (doc/ 폴더)

작업 성격에 맞는 문서를 참조하세요.

| 문서 | 내용 |
|---|---|
| [doc/01-overview.md](doc/01-overview.md) | 프로젝트 개요, 목표, 범위(v1 scope) |
| [doc/02-architecture.md](doc/02-architecture.md) | 프로젝트 구조, MVVM 설계, 폴더/네임스페이스 구성 |
| [doc/03-data-storage.md](doc/03-data-storage.md) | .data 폴더 구조, 파일명 규칙, 순서-파일명 동기화(재넘버링) 로직 |
| [doc/04-topic-management.md](doc/04-topic-management.md) | 대주제/소주제 트리 UI, 추가/삭제/이름변경(cascading 처리 포함) |
| [doc/05-image-features.md](doc/05-image-features.md) | 이미지 드래그드롭 추가/이동, 순서 변경, 지원 포맷 |
| [doc/06-view-modes.md](doc/06-view-modes.md) | 연속보기/단일보기 모드, 좌우 네비게이션 |
| [doc/07-ui-layout.md](doc/07-ui-layout.md) | 전체 화면 레이아웃, 주요 컨트롤 구성 |
| [doc/08-open-decisions.md](doc/08-open-decisions.md) | 아직 결정되지 않은 항목, 향후 확장 아이디어 |

## 작업 원칙
1. 각 기능 구현 전 해당 보조 지시서를 먼저 확인한다.
2. 보조 지시서 간 내용이 충돌하면 사용자에게 확인 후 진행한다.
3. `doc/08-open-decisions.md`에 있는 미결정 항목은 임의로 확정하지 말고, 합리적 기본값을 제안하되 사용자 확인을 받는다.
4. 코드는 MVVM 패턴을 따르며, View(XAML)와 로직(ViewModel/Service)을 분리한다.
5. 기능/구조/결정 사항이 변경되면 해당 보조 지시서(doc/ 폴더)를 함께 업데이트한다. 문서와 코드가 어긋난 상태로 두지 않는다.
6. 사용자 명령으로 파일이 수정되면, 별도 요청 없이도 알아서 git commit(적절한 커밋 메시지 자동 작성)과 push까지 수행한다.
