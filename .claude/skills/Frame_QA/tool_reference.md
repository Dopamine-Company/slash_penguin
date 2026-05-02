# Frame_QA 도구 레퍼런스

## Unity 도구 레퍼런스 (CLI 기반)

> **Frame_QA에서 사용할 수 있는 Unity 도구 전체 목록.**
> CLI 호출: `node .mcp-server/unity-cli.mjs [도구명] '[JSON params]'`
> MCP 폴백: `mcp__unity__[도구명]` (CLI 실패 시에만, `[BATCH_FALLBACK]` 기록)
> `execute_code`는 **Play Mode에서 동작**한다.

### 조작 도구 (Play Mode 필수)

| 도구 | 파라미터 | Frame_QA 용도 |
|------|---------|--------------|
| **`click_ui_element`** | `gameObjectPath` 또는 `instanceId` | EventSystem 경유 UI 클릭 (Enter→Down→Up→Click 전체 시퀀스) |
| **`simulate_input`** | `action` (keyDown/keyUp/mouseClick/mouseMove/hold), `key`, `position`, `duration` | New Input System 기반 키보드/마우스 입력. Legacy Input 폴백 포함 |
| **`set_ui_input`** | `gameObjectPath`/`instanceId`, `text` | InputField/TMP_InputField 텍스트 설정 (onValueChanged/onEndEdit 이벤트 발행) |
| **`set_play_mode`** | `play` (bool) | Play Mode 진입/탈출. 비동기 — `get_state`로 전환 완료 확인 필요 |

### 조회 도구

| 도구 | 파라미터 | Frame_QA 용도 |
|------|---------|--------------|
| **`get_state`** | (없음) | isPlaying, 열린 씬 목록, Unity 버전 확인 |
| **`get_scene_hierarchy`** | `rootPath`(선택), `maxDepth`(기본10), `includeInactive`(기본true) | 씬 전체 오브젝트 트리. 컴포넌트 목록 포함. Phase 2에서 UI 구조 확인용 |
| **`get_component_data`** | `gameObjectPath`/`instanceId`, `componentType` | SerializedObject 기반 컴포넌트 필드/프로퍼티 읽기. **C# 리플렉션 대체** |
| **`get_ui_state`** | `gameObjectPath`/`instanceId` | Button/Toggle/Slider/Dropdown/InputField 상태 + RectTransform 위치. 검증용 |
| **`find_ui_elements`** | `filter`(컴포넌트 타입 필터) | 전체 Canvas 하위 UI 요소 탐색. rect/interactable/textContent/depth 반환 |
| **`find_objects_by_criteria`** | `tag`, `layer`, `componentType`, `nameContains` | 조건부 오브젝트 검색 (최대 200개). 프로젝트 타입명으로 검색 가능 |
| **`get_console_logs`** | `filter`(error/warning/log), `since`(ms), `clear`(bool), `maxCount`(기본100) | Console 로그 수집/클리어. Phase 2 에러 감지 핵심 |
| **`inspect_ui_layout`** | `screenWidth`(기본1920), `screenHeight`(기본1080) | **UI 레이아웃 자동 검증**: 화면 밖 밀림, 터치 타겟 크기, 텍스트 넘침, Selectable 오버랩 감지 |
| **`analyze_scene`** | (없음) | Renderer/Material/Shader/Light 분석. missing material(핑크 텍스처) 감지 |

### 스크린샷/코드 실행

| 도구 | 파라미터 | Frame_QA 용도 |
|------|---------|--------------|
| **`screenshot`** | `view` ("scene" 또는 **"game"**) | Game View 캡처 → Vision 분석 |
| **`execute_code`** | `code` (C# 코드 문자열) | **Play Mode에서 동작**. 상태 읽기, 조건 폴링, Lua 래퍼 실행 |

### 도구 선택 우선순위 (CLI-first 원칙)

1. **CLI 배치 스크립트 최우선**: 반복/폴링/병렬 가능한 작업은 CLI 배치로 실행 (**토큰 0, 속도 3-5배**)
2. **전용 MCP 도구**: CLI 배치에 포함되지 않는 **1회성 조회** 또는 **Claude 판단이 필요한 작업**만 MCP
3. **execute_code는 최후 수단**: 위 방법으로 불가능한 경우에만 사용

**CLI vs MCP 판단 기준**: "결과를 미리 알고 있는가?" → YES면 CLI, NO면 MCP

---

## CLI 배치 도구 레퍼런스 (`.mcp-server/qa-*.mjs`)

> **MCP 도구를 WebSocket 병렬 처리로 묶은 CLI 스크립트.**
> Unity Editor에 **멀티 커넥션**으로 동시 요청하므로 MCP 직렬 호출 대비 3-5배 빠르다.
> `Bash` 도구로 호출하며, 모든 출력은 **JSON** (stdout). 에러도 JSON으로 반환 (exit 0).

### 핵심 스크립트

| 스크립트 | CLI 예시 | 대체하는 MCP 호출 수 |
|----------|---------|---------------------|
| **`qa-scene-entry.mjs`** | `node .mcp-server/qa-scene-entry.mjs --scene "StoreScene"` | 7 → 1 (내부 4병렬) |
| **`qa-poll.mjs`** | `node .mcp-server/qa-poll.mjs --code-file _qa/poll/check.cs --expect "POLL:OK"` | 최대 100 → 1 |
| **`qa-click.mjs`** | `node .mcp-server/qa-click.mjs --name "시작 버튼" --wait 500` | 3 → 1 |
| **`qa-step.mjs`** | `node .mcp-server/qa-step.mjs --step-file _qa/steps/step_03.json` | 8+ → 1 |
| **`qa-runner.mjs`** | `node .mcp-server/qa-runner.mjs --plan _qa/plans/scenario.json` | 전체 Phase 2 → 1 |

### qa-scene-entry.mjs — 씬 진입 검증 (병렬)

```bash
node .mcp-server/qa-scene-entry.mjs --scene "StoreScene" --timeout 15000 --play false
```

**인자:**
- `--scene` (필수): 기대 씬 이름
- `--timeout` (기본 15000): 씬 대기 최대 ms
- `--play` (기본 "false"): "true"면 Play Mode 진입 후 대기

**내부 흐름:**
1. get_state 폴링 (씬 이름 일치까지)
2. 씬 도달 후 **4개 병렬** 실행:
   - `inspect_ui_layout` + `find_objects_by_criteria(EventSystem)` + `screenshot` + `get_console_logs`

**반환 JSON:**
```json
{
  "success": true, "scene": "StoreScene", "pollCount": 3,
  "uiLayout": {...}, "eventSystemCount": 1,
  "screenshot": {...}, "consoleErrors": []
}
```

### qa-poll.mjs — 비동기 조건 폴링

```bash
node .mcp-server/qa-poll.mjs --code-file _qa/poll/check_turn.cs --expect "POLL:OK" --timeout 15000
node .mcp-server/qa-poll.mjs --code "return \"POLL:OK\";" --expect "POLL:OK" --snapshot
```

**인자:**
- `--code` 또는 `--code-file` (필수): C# 폴링 코드
- `--expect` (필수): 결과에서 매칭할 문자열
- `--interval` (기본 300): 폴링 간격 ms
- `--timeout` (기본 15000): 최대 대기
- `--snapshot` (플래그): 성공 시 screenshot + logs 병렬 수집

**반환 JSON:**
```json
{ "success": true, "pollCount": 8, "pollTimeMs": 2400, "result": "POLL:OK:Turn=2" }
```

### qa-click.mjs — UI 클릭 배치

```bash
node .mcp-server/qa-click.mjs --name "시작 버튼" --wait 500
node .mcp-server/qa-click.mjs --path "Canvas/MainMenu/StartButton"
```

**인자:**
- `--name` 또는 `--path` (필수): 대상 UI 요소
- `--wait` (기본 500): 클릭 후 대기 ms
- `--check-errors` (기본 true): 클릭 후 콘솔 에러 확인

**우선순위:** exact name+interactable > exact name > partial+interactable > first candidate

### qa-step.mjs — Step 통합 실행

```bash
node .mcp-server/qa-step.mjs --step-file _qa/steps/step_03.json
```

**Step JSON 포맷:**
```json
{
  "id": 3, "node": "STORE_NPC_CLICK",
  "action": { "type": "click", "target": { "name": "고페르테" }, "wait": 500 },
  "poll": { "codeFile": "_qa/poll/check_combat.cs", "expect": "POLL:OK", "timeout": 15000 },
  "verify": { "lua": "_qa/verify/store_to_combat.lua" },
  "screenshot": true, "checkErrors": true
}
```

**action.type:** `click` | `input` | `wait` | `play`

### qa-runner.mjs — 전체 시나리오 실행기 (auto=true 추천)

```bash
node .mcp-server/qa-runner.mjs --plan _qa/plans/scenario_store_to_combat.json --timeout 300000
```

**Plan JSON 포맷:**
```json
{
  "scenario": "store_to_combat",
  "playMode": true,
  "steps": [ ...step objects... ]
}
```

**반환 JSON:**
```json
{
  "scenario": "store_to_combat", "success": true,
  "totalSteps": 8, "passed": 7, "failed": 1,
  "steps": [...], "screenshots": ["path1", "path2"],
  "allConsoleErrors": [], "totalTimeMs": 45000
}
```

### CLI vs MCP 선택 기준 (토큰/속도 최적화)

| 상황 | 사용 도구 | 토큰 | 속도 |
|------|----------|------|------|
| 씬 전환 + 검증 | **CLI** `qa-scene-entry.mjs` | 0 | ~100ms (4병렬) |
| 조건 대기 (턴 변경, 애니메이션 등) | **CLI** `qa-poll.mjs` | 0 | 폴링 횟수 × ~300ms |
| 버튼/UI 클릭 | **CLI** `qa-click.mjs` | 0 | ~50ms |
| 실행 스크립트 Step 전체 | **CLI** `qa-step.mjs` | 0 | Step당 ~2s |
| **auto=true 시나리오 전체** | **CLI** `qa-runner.mjs` | **0** | **전체 ~30-60s** |
| 스크린샷 Vision 분석 | **Claude** `Read` 이미지 | ~500 | Claude 추론 |
| 실패 원인 분석/복구 | **Claude** MCP 도구 | ~200/회 | Claude 추론 |
| CLI 전체 실패 시 | **MCP 폴백** + `[BATCH_FALLBACK]` | ~200/Step | 느림 |

**예상 토큰 절감**: auto=true 10사이클 기준
- 이전 (전부 MCP): ~50,000 토큰/사이클 × 10 = ~500,000
- **CLI-first**: ~5,000 토큰/사이클 (Vision만) × 10 = ~50,000 (**90% 절감**)
