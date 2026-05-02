# slash_penguin

Claude Code가 이 프로젝트에서 작업할 때 참고하는 컨텍스트 파일입니다. (이 파일은 git에 커밋해서 팀과 공유하세요.)

## 프로젝트 개요

- **이름**: slash_penguin
- **조직**: Dopamine-Company
- **저장소**: https://github.com/Dopamine-Company/slash_penguin.git
- **엔진**: Unity 6000.3.11f1 (Unity 6)
- **언어**: C# (.NET / Unity)
- **플랫폼**: (TBD — 모바일/PC 등 정해지면 여기 적기)
- **장르**: (TBD — 게임 컨셉 한 줄)

## 디렉토리 구조

```
slash_penguin/
├── Assets/                  # 게임 에셋, 씬, 스크립트, 프리팹 (현재 비어있음 / 작업 시작점)
├── Packages/
│   ├── manifest.json        # Unity 패키지 의존성
│   └── com.dopamine.slash-penguin/   # 로컬 임베디드 패키지 (게임 코드는 여기에 둘 수도 있음)
├── ProjectSettings/         # Unity 프로젝트 설정 (Editor 버전 등)
├── Library/                 # Unity 자동 생성 (gitignore)
├── Temp/, Logs/, Build/     # gitignore 대상
├── CLAUDE.md                # ← 이 파일
├── .mcp.json                # Claude Code의 MCP 서버 설정
└── .gitignore
```

## 의존성 (Packages/manifest.json 핵심)

- `com.dopamine.slash-penguin` (로컬 임베디드 패키지)
- `com.unity.multiplayer.center` 1.0.1
- 표준 Unity 모듈들 (physics, physics2d, animation, audio, ui, uielements, terrain, vehicles, vr, xr, vectorgraphics, webrequest 등)

## 코딩 컨벤션

- **클래스 / 메서드 / 프로퍼티**: PascalCase
- **로컬 변수 / 매개변수**: camelCase
- **private 필드**: `_camelCase` (언더스코어 prefix)
- **상수**: PascalCase 또는 SCREAMING_SNAKE_CASE (팀 합의 후 통일)
- **네임스페이스**: `Dopamine.SlashPenguin.<Feature>` 형태 권장
- **파일명 = 클래스명** (Unity 규칙)
- 한 파일에 여러 public 클래스 두지 않기

## Unity 작업 가이드라인

- MonoBehaviour는 입력/이벤트 진입점 역할만 하고, 게임 로직은 일반 클래스로 분리하기
- 데이터는 ScriptableObject로 관리
- 매 프레임 GetComponent / Find 호출 금지 — Awake/Start에서 캐싱
- 문자열 비교나 SendMessage 대신 이벤트/델리게이트 사용
- 씬 간 공유 데이터는 싱글톤보다 ScriptableObject 또는 의존성 주입 선호
- UI는 UI Toolkit (UIElements) 또는 uGUI 중 하나로 통일 (현재 양쪽 다 모듈 활성화 상태이므로 팀에서 결정)

## 빌드 / 실행

- Unity Hub에서 `slash_penguin` 프로젝트를 Unity **6000.3.11f1** 버전으로 열기
- Play 모드 진입은 Editor에서 직접
- 빌드 결과물은 `/Build` (gitignore)

## Git

- 기본 브랜치: `main` (가정 — 다르면 수정)
- `.csproj`, `.sln`, `Library/`, `Temp/`, `Logs/`, `UserSettings/` 모두 gitignore 처리됨
- LFS 사용 시: 큰 바이너리(텍스처/오디오/모델)는 `.gitattributes`에 등록 필요 (현재 미설정)

## Claude Code 작업 시 주의

- **MonoBehaviour 새 클래스를 만들면** Unity Editor가 다시 컴파일해야 인식하므로, 파일 생성 후 Editor 창을 한 번 활성화시키기
- **`.meta` 파일**은 절대 임의로 수정·삭제하지 말 것 (Unity가 자동 관리)
- **씬 파일(.unity)이나 prefab(.prefab)** 은 YAML이지만 손으로 편집하지 말고 Editor에서 다루기 — 꼭 필요할 때만 텍스트 패치
- 새 C# 스크립트는 기본적으로 `Assets/Scripts/` 아래에 생성 (없으면 만들기)
- 게임 도메인 코드를 로컬 패키지(`Packages/com.dopamine.slash-penguin/`)에 둘지 `Assets/`에 둘지 — 팀에서 결정 후 여기에 명시

## MCP / 도구

- `.mcp.json`에 Unity MCP 서버 자리가 잡혀 있음 (자세한 설정은 그 파일 참고)
- 추천 Unity MCP: [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)
  - Unity Package Manager에서 Coplay 패키지 설치 → 브리지 서버 실행 → `.mcp.json`의 `command`/`args` 업데이트
  - 또는 공식 Unity AI Assistant (`com.unity.ai.assistant`) 사용 가능

## 자주 쓰는 작업 예시 (Claude Code 프롬프트 템플릿)

- "`Assets/Scripts/Player/PlayerController.cs` 만들어줘 — Rigidbody2D 기반 좌우 이동 + 점프"
- "`Assets/Scripts/`의 C# 코드 전체 훑고 GetComponent 매 프레임 호출하는 곳 찾아서 캐싱하게 리팩터"
- "Packages/com.dopamine.slash-penguin에 ScriptableObject로 PenguinStats 정의"

## 메모

- 프로젝트가 아직 초기 단계 — Assets/ 폴더와 첫 씬이 비어있음
- 컨셉/기획이 정해지면 이 파일 상단의 "프로젝트 개요" 섹션을 채울 것
