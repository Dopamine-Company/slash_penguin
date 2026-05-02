# Phase 1: Plan 모드 — 전체 플로우 분석 & 실행 계획 수립

> **auto=false (기본)**: 반드시 `EnterPlanMode`로 시작한다.
> **auto=true**: `EnterPlanMode`를 호출하지 않는다. Plan 분석은 동일하게 수행하되, 사용자 승인 없이 자동 진행한다.
> 이 Phase에서 코드를 읽고 분석하여 모든 게임 플로우, UI 플로우, 상태 전환을 파악한다.

> ⚠️ VERIFY: Phase 1에서 Play Mode에 진입하지 않는다.

## 성숙도별 Phase 1 실행 분기

> Phase 0의 성숙도 판정 결과에 따라 분기한다.

**성숙 (항목 5개 이상):**
- 1~3단계 병렬 에이전트를 **실행하지 않는다**
- 4단계(검증 포인트 도출)를 **기존 verify 스크립트의 항목을 그대로 사용**하여 즉시 수행
- 5단계(Lua 스크립트 계획)는 "유지" — 기존 스크립트 수정 없음
- 6단계(실행 스크립트 작성)만 수행
- game_flow_analysis.md가 있으면 이것과 기존 verify 스크립트만으로 Plan을 완성한다

**성장중 (항목 1~4개):**
- 1~3단계 중 **에이전트 1개만 실행**: 에이전트 B(게임 매니저/상태 머신 분석)
  - 이전 QA 이후 변경된 코드가 있는지 확인하는 것이 목적
  - `Grep`으로 `_qa/verify/` 스크립트에 언급된 타입명을 검색하여 변경 여부 판단
- 4단계: 기존 검증 포인트 + 변경 감지된 부분만 추가
- 5단계: 보강 모드
- 6단계: 수행

**신규 (스크립트 없음):**
- 기존과 동일. 1~6단계 전체 수행.

## 1~3단계: 병렬 분석 (Explore 에이전트 3개 동시 launch)

> Explore 에이전트 3개를 `Task` 도구로 동시에 launch한다.
> 세 에이전트는 독립적이므로 반드시 **하나의 메시지에서 세 Task 호출을 병렬로** 보낸다.
> 각 에이전트에 `max_turns: 30` 이상을 부여하여 충분히 탐색하게 한다.

### 에이전트 A — 1단계: 씬 및 게임 시스템 파악
프롬프트에 다음 지시를 포함한다:
- `Glob`으로 `Assets/Scenes/*.unity` 패턴 검색 → 씬 파일 목록 수집
- 인자로 특정 시나리오가 지정된 경우 해당 범위만 대상으로 한정
- `Grep`으로 `SceneManager.LoadScene` 호출 검색 → 씬 간 전환 경로 추출
- 씬 전환 그래프 작성: `씬A → [조건] → 씬B`

### 에이전트 B — 2단계: 게임 매니저/상태 머신 분석
프롬프트에 다음 지시를 포함한다:
- `Grep`으로 `GameManager`, `BattleManager`, `TurnManager`, `StateManager` 등 핵심 매니저 클래스 검색
- `Read`로 매니저 코드를 읽어 상태 전환 로직, 이벤트 흐름 파악
- 주요 이벤트 흐름 매핑 (예: 전투 시작 → 턴 진행 → 전투 종료)
- 각 상태에서 가능한 액션과 전환 조건 정리

### 에이전트 C — 3단계: UI 플로우 + UI 레이어 검증 + 데이터 파일 분석
프롬프트에 다음 지시를 포함한다:

**UI 플로우 분석:**
- `Grep`으로 `Canvas`, `Panel`, `Dialog`, `Popup`, `UIManager`, `UIController` 등 UI 클래스 검색
- `Read`로 UI 매니저/컨트롤러 코드를 읽어 UI 전환 로직 파악
- 각 UI 화면(패널/다이얼로그)의 열림/닫힘 조건 매핑
- 버튼 OnClick 이벤트 바인딩 추적:
  - 코드의 `onClick.AddListener` 호출
  - `[SerializeField]` 버튼 필드 + Inspector 연결 추정
- `CanvasGroup` 사용처 검색 → 반투명/페이드 연출 UI 식별
- `DOTween`, `LeanTween`, `Animator` 등 UI 애니메이션 연출 파악
- **UI 전환 시퀀스 순서도** 작성:
  ```
  예) 메인메뉴 → [시작 버튼 클릭] → 로딩패널(CanvasGroup 페이드인) → 게임씬 로드
      전투씬 → [스킬 버튼] → 스킬선택패널 열림 → [스킬 선택] → 대상선택 → [대상 선택] → 스킬 실행
  ```
- **각 UI 요소의 접근 경로** 정리:
  - 오브젝트 이름, 계층 경로, 활성화 조건
  - 예) `Canvas/BattleUI/SkillPanel/SkillButton_0` — 전투 상태에서만 활성

**UI 레이어 검증:**
- `Grep`으로 C# 코드에서 `sortingOrder` 할당 검색
- `Grep`으로 `.prefab` 및 `.unity` 파일에서 `m_SortingOrder` 값 수집
- Canvas 동적 생성 코드 검색: `AddComponent<Canvas>`, `Instantiate(.*Canvas`
- `RenderMode` 설정 검색 및 일관성 확인
- `EventSystem` 존재 여부 및 복수 생성 가능성 확인
- 결과를 **UI 레이어 맵 테이블** 형식으로 정리

**데이터 파일(XML/Lua) 분석:**
- `Glob`으로 `**/*.xml`, `**/*.lua` 패턴 검색 → 프로젝트 내 데이터 파일 목록 수집
- QA 시나리오 실행에 필요한 데이터 파일 식별 (예: Guest.xml, Encounter.xml, Card.lua 등)
- 각 데이터 파일의 경로, 역할, 필수 키/필드 정리
- **누락된 데이터 파일 목록** 작성 — QA 시나리오에 필요하지만 존재하지 않거나 특정 엔트리가 부족한 경우

**Lua 접근 가능 API 분석:**
- Grep으로 `luaEnv.Global.Set` 검색 → Lua에 등록된 C# 함수 목록 수집
- Grep으로 `[LuaCallCSharp]` 검색 → XLua 바인딩 대상 타입 식별
- Assets/XLua/Gen/ 디렉토리의 *Wrap.cs 파일 목록 → 자동 생성 바인딩 확인
- 각 QA 검증 포인트에서 필요한 게임 상태가 Lua로 접근 가능한지 판별

## 에이전트 결과 수신 후 순차 수행

## 4단계: 검증 포인트 도출
에이전트 A~C 결과를 종합하여:
- **상태 전환**: 전투 시작 시 HP 초기화, 턴 전환 시 행동력 리셋 등
- **데이터 무결성**: 아이템 사용 후 인벤토리 반영, 골드 증감 등
- **UI 전환**: 패널 열림/닫힘, 반투명 오버레이, 버튼 활성화 등
- **비주얼**: 스킬 이펙트, 애니메이션, UI 레이아웃 깨짐 등
- **UI 레이어**: sortingOrder 충돌, RenderMode 불일치, EventSystem 누락 등

**각 검증 포인트에 검증 레벨을 명시한다:**

| Level | 범위 | 검증 방법 | 실패 시 처리 |
|-------|------|----------|-------------|
| 1 | 컴파일/로드 | execute_code 컴파일 성공, 씬 로드 확인 | Step 중단, 에러 기록 |
| 2 | 런타임 예외 | null 참조, 예외 발생 여부, console error 체크 | 이슈 기록 후 계속 |
| 3 | 로직 정합성 | 기대값 vs 실제값, HP 변화, 턴 전환, GC Alloc | 이슈 기록 후 계속 |
| 4 | Vision (시각 정합성) | 스크린샷 캡처 → Vision 프롬프트 분석 → 카테고리별 이상 탐지 | 이슈 기록 후 계속 |

```markdown
## 검증 포인트 예시
| # | 항목 | Level | 기대값 | 검증 방법 |
|---|------|-------|--------|----------|
| 1 | 전투 씬 로드 | L1 | sceneName == "전투 Scene" | get_state 폴링 |
| 2 | CombatDirector null 아님 | L2 | not null | safe_check |
| 3 | Turn == 2 (턴 종료) | L3 | Turn.Base == 2 | safe_check + 폴링 |
| 4 | Console 에러 없음 | L2 | error count == 0 | get_console_logs |
| 5 | 전투 UI 정상 표시 | L4 | VISION_CLEAR | screenshot + 전투 UI 특화 프롬프트 |
| 6 | HP 바 "0/0" 아님 | L4 | HP 값 정상 표시 | screenshot + State 카테고리 검증 |
```

**Level 4 (Vision) 검증 포인트 작성 규칙:**
- 각 Step에 Vision 검증 필요 여부를 명시한다: `Vision: Yes (카테고리: Layout, Text, State)` 또는 `Vision: No`
- Vision 검증이 필요한 Step에는 적용할 프롬프트 템플릿 번호(1/2/3)를 명시한다
- 비교 분석(템플릿 3)이 필요한 경우 비교 대상 스크린샷 시점을 명시한다
- Vision 발견 이슈는 카테고리별로 분류하여 보고서에 포함한다

## 5단계: 임시 데이터 파일 + _qa/ Lua 검증 스크립트 계획

에이전트 C의 데이터 파일 분석 결과를 바탕으로, QA 실행에 필요한 **임시 파일 생성 계획**과 **_qa/ Lua 스크립트 계획**을 수립한다.

```markdown
## 임시 데이터 파일 계획

### 생성할 파일
| 파일 경로 | 목적 | 원본 존재 | 처리 |
|-----------|------|-----------|------|
| Assets/Data/TestGuest.xml | QA용 테스트 손님 데이터 | No | 신규 생성 → QA 후 삭제 |
| Assets/Data/Guest.xml | 기존 파일에 테스트 엔트리 추가 | Yes | 백업 → 수정 → QA 후 원본 복원 |

### 파일 내용
#### [파일 경로]
```xml
[실제 생성할 파일 내용]
```
```

**규칙:**
- 기존 파일 수정 시: 반드시 원본을 `.bak` 확장자로 백업한 뒤 수정
- 신규 파일 생성 시: 경로를 `_tempQA_created` 목록에 기록하여 Phase 3에서 삭제
- 파일 내용은 Plan에 **전문(full content)**을 명시하여 Phase 2에서 그대로 생성

### _qa/ Lua 검증 스크립트 계획

#### 모드 판별
- **신규 생성**: `_qa/verify/` 에 해당 시나리오 스크립트 없음 → 전체 작성
- **보강**: 기존 스크립트 존재 → Read로 읽어서 부족한 검증 항목만 추가 계획

#### 스크립트 계획 예시
| 스크립트 | 모드 | 변경 내용 |
|----------|------|----------|
| _qa/lib/common.lua | 신규/유지 | check(), info() 유틸 (이미 있으면 건너뜀) |
| _qa/lib/ui_verify.lua | 신규/유지 | Canvas, EventSystem 검증 |
| _qa/verify/store_scene.lua | 보강 | 손님 선택 검증 추가 (기존 인카운터 검증은 유지) |
| _qa/verify/combat_entry.lua | 신규 | PendingCombatKey, 씬 전환 검증 |

#### lib/common.lua 표준 내용
```lua
-- _qa/lib/common.lua
local M = {}
M.results = {}

function M.check(name, actual, expected)
    local pass = (tostring(actual) == tostring(expected))
    M.results[#M.results+1] = string.format("[%s] %s: actual=%s, expected=%s",
        pass and "PASS" or "FAIL", name, tostring(actual), tostring(expected))
end

function M.safe_check(name, fn, expected)
    local ok, result = pcall(fn)
    if not ok then
        M.results[#M.results+1] = string.format("[ERROR] %s: %s", name, tostring(result))
    else
        M.check(name, result, expected)
    end
end

function M.info(name, value)
    M.results[#M.results+1] = string.format("[INFO] %s: %s", name, tostring(value))
end

function M.safe_info(name, fn)
    local ok, result = pcall(fn)
    if not ok then
        M.results[#M.results+1] = string.format("[ERROR] %s: %s", name, tostring(result))
    else
        M.info(name, result)
    end
end

function M.report()
    return table.concat(M.results, "\n")
end

function M.reset()
    M.results = {}
end

return M
```

#### lib/bootstrap.lua 표준 내용
```lua
-- _qa/lib/bootstrap.lua
-- QA용 LuaEnv 헬퍼 — 프로젝트 기존 LuaEnv의 커스텀 바인딩을 사용할 수 없으므로 자체 등록

local env = {}

-- 타입 이름으로 오브젝트 찾기 (리플렉션 모드 fallback)
env.find = function(typeName)
    local ok, result = pcall(function()
        return CS.UnityEngine.Object.FindAnyObjectByType(
            CS.System.Type.GetType(typeName))
    end)
    if ok then return result end
    -- fallback: MonoBehaviour 순회
    local allMbs = CS.UnityEngine.Object.FindObjectsByType(
        typeof(CS.UnityEngine.MonoBehaviour),
        CS.UnityEngine.FindObjectsSortMode.None)
    for i = 0, allMbs.Length - 1 do
        if allMbs[i]:GetType().Name == typeName then
            return allMbs[i]
        end
    end
    return nil
end

-- 프로퍼티 체인 접근 (obj, "Prop1.Prop2.Prop3")
env.get = function(obj, propChain)
    local current = obj
    for prop in string.gmatch(propChain, "[^%.]+") do
        if current == nil then return nil end
        local ok, val = pcall(function()
            return current:GetType():GetProperty(prop):GetValue(current)
        end)
        if not ok then return nil end
        current = val
    end
    return current
end

-- 씬 이름, FPS, 메모리 등 기본 유틸
env.sceneName = function()
    return CS.UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
end
env.fps = function() return 1.0 / CS.UnityEngine.Time.deltaTime end
env.memoryMB = function()
    return CS.UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)
end

return env
```

#### verify 스크립트 표준 구조
```lua
-- _qa/verify/combat_entry.lua
local qa = dofile(CS.UnityEngine.Application.dataPath .. "/../_qa/lib/common.lua")
local env = dofile(CS.UnityEngine.Application.dataPath .. "/../_qa/lib/bootstrap.lua")
qa.reset()

-- === 검증 항목 (QA 반복으로 점진 추가) ===
-- 모든 검증은 safe_check/safe_info로 감싸 에러 격리

-- v1: 초기 생성 (L1 — 씬 로드 확인)
qa.check("씬이름", env.sceneName(), "전투 Scene")

-- v2: 2회차 QA에서 추가 (L2 — 런타임 null 체크)
qa.safe_check("CombatDirector 존재", function()
    return env.find("CombatDirector") ~= nil
end, true)

-- v3: 3회차 QA에서 추가 (L3 — 로직 검증)
qa.safe_check("Turn == 1", function()
    local director = env.find("CombatDirector")
    return tostring(env.get(director, "Current.Turn.Base"))
end, "1")

-- v4: bootstrap.lua 헬퍼 활용 예시
qa.safe_info("FPS", env.fps)
qa.safe_info("메모리(MB)", function() return string.format("%.1f", env.memoryMB()) end)

return qa.report()
```

**safe_check vs check 사용 기준:**
- `check`: 실패해도 괜찮은 단순 비교 (이미 null 체크된 값)
- `safe_check`: 예외 가능성이 있는 검증 (리플렉션, 타입 접근, 연산)
- `safe_info`: 예외 가능성이 있는 정보 수집 (FPS, 메모리, 프로퍼티 체인)

## 6단계: 실행 스크립트 작성
Plan 파일에 **플레이 모드에서 기계적으로 실행할 스크립트**를 작성한다.
플레이 모드에서는 이 스크립트를 순서대로 따라가기만 하면 된다.

> **Mermaid 노드 참조 필수**: Phase 0.5의 "노드 → Step 매핑 테이블"을 기반으로,
> 각 Step에 대응하는 Mermaid 노드 ID를 반드시 명시한다.
> 이 노드 ID가 Phase 3의 Figma 결과 다이어그램에서 PASS/FAIL 색칠과 커버리지 계산에 사용된다.
> 노드 ID가 없는 Step은 커버리지에서 제외되므로, 모든 Step에 노드 ID를 부여한다.

> 각 Step에 관측 비용을 명시하여, 읽기 전용 → 상태 변경 → 직접 조작 순서로 단계적으로 올린다.

```markdown
## 실행 스크립트

### 시나리오: [시나리오명]

#### Step 1: [설명] — 노드: [MERMAID_NODE_ID]
- 액션: `click_ui_element` — 대상: "[오브젝트 경로 또는 이름]"
- 대기: wait_for_seconds 0.5초
- **Lua 배치 검증**: `_qa/verify/[시나리오명].lua` 실행
  - 검증 항목: [항목1], [항목2], [항목3]
- 스크린샷: Yes

#### Lua 실행 래퍼 (execute_code):
```csharp
var luaEnv = new XLua.LuaEnv();
try {
    var luaScript = System.IO.File.ReadAllText(
        UnityEngine.Application.dataPath + "/../_qa/verify/[시나리오명].lua");
    var results = luaEnv.DoString(luaScript);
    return results != null && results.Length > 0 ? results[0].ToString() : "no results";
} catch (System.Exception e) {
    return "ERROR: " + e.Message;
} finally {
    luaEnv.Dispose();
}
```

#### Step 2: [설명] — 노드: [MERMAID_NODE_ID]
...

### UI 레이어 런타임 검증 Step
- 액션: **`inspect_ui_layout`** — UI 레이아웃 자동 검증 (execute_code 대체)
  ```
  inspect_ui_layout(screenWidth: 1920, screenHeight: 1080)
  ```
- 반환값에서 자동 감지되는 항목:
  - `off_screen` — 화면 밖으로 밀린 UI 요소
  - `touch_target_too_small` — 88px 미만 터치 타겟 (Selectable)
  - `text_overflow` — 텍스트가 컨테이너를 넘침 (Text + TMP_Text)
  - `overlap` — 서로 다른 Selectable 요소 간 의도치 않은 겹침 (부모-자식 제외)
- 추가 검증 (inspect_ui_layout이 커버하지 않는 항목):
  ```
  get_scene_hierarchy(rootPath: "[Canvas경로]", maxDepth: 3)
  → Canvas 구조에서 EventSystem 존재 여부 확인
  find_objects_by_criteria(componentType: "EventSystem")
  → EventSystem이 정확히 1개인지 확인 (0개: Critical, 2개+: Warning)
  ```

### 반투명 UI 목록
| UI 요소 | CanvasGroup 경로 | 처리 방법 |
|---------|-----------------|----------|
| 로딩 오버레이 | Canvas/LoadingOverlay | alpha 폴링으로 완료 대기 |
| ... | ... | ... |
```

**스크립트 작성 규칙:**
- 각 Step에 실행할 MCP 도구와 파라미터를 구체적으로 명시
- `execute_code`에 넣을 C# 코드는 그대로 복사-붙여넣기 가능하게 작성
- 조건 대기가 필요한 경우 폴링 코드와 타임아웃 명시
- 스크린샷 캡처 시점 명시 (비주얼 검증이 필요한 단계만)
- **플로우 판단 로직을 절대 포함하지 않는다** — 모든 분기는 Plan에서 결정
- **UI 레이어 런타임 검증 Step을 반드시 포함한다** — 각 씬 진입 후 1회 실행
- **각 Step에 검증 레벨(L1/L2/L3)을 명시한다**
- **비동기 대기가 필요한 Step은 "폴링 → Lua 검증" 시퀀스를 사용한다**

### 비동기 폴링 → Lua 검증 표준 시퀀스

비동기 대기가 필요한 Step에서는 반드시 다음 패턴을 따른다:

```markdown
#### Step N: [설명] (비동기)
- **Phase A — C# 폴링 (조건 대기)**:
  - `execute_code`로 조건 확인 (예: 씬 전환 완료, 턴 변경)
  - 실패 시: `wait_for_seconds(0.3)` 후 재시도
  - 최대 재시도: 15초 (약 50회) / 타임아웃 시 현재 상태 스냅샷 + FAIL 기록
- **Phase B — Lua 배치 검증 (폴링 성공 후)**:
  - `_qa/verify/[시나리오].lua` 실행
  - 모든 검증 항목은 `safe_check`/`safe_info`로 감싸 에러 격리
- **Phase C — 실패 시 스냅샷**:
  - 폴링 타임아웃 또는 Lua 에러 시:
    - `screenshot(view: "game")` — 현재 화면 캡처
    - `get_console_logs(filter: "error")` — 에러 로그 수집
    - 이슈로 기록 후 다음 Step 진행
```

**C# 폴링 코드 표준 패턴 (execute_code):**
```csharp
// 예: 턴 번호가 변경될 때까지 대기 (단일 실행 — 외부에서 wait_for_seconds로 재시도)
var allMbs = UnityEngine.Object.FindObjectsByType(typeof(UnityEngine.MonoBehaviour), UnityEngine.FindObjectsSortMode.None);
UnityEngine.MonoBehaviour director = null;
foreach (var mb in allMbs) {
    if (mb.GetType().Name == "CombatDirector") { director = mb as UnityEngine.MonoBehaviour; break; }
}
if (director == null) return "POLL:FAIL:CombatDirector not found";
var instance = director.GetType().GetProperty("Current").GetValue(director);
if (instance == null) return "POLL:FAIL:Instance null";
var turn = instance.GetType().GetProperty("Turn").GetValue(instance);
var baseTurn = turn.GetType().GetProperty("Base").GetValue(turn);
return "POLL:" + (baseTurn.ToString() == "2" ? "OK" : "WAIT") + ":Turn=" + baseTurn;
```

**실행 루프에서의 폴링 패턴:**
```
반복 (최대 15초):
  1. execute_code → 결과 확인
  2. "POLL:OK" → Phase B (Lua 검증) 진행
  3. "POLL:WAIT" → wait_for_seconds(0.3) 후 재시도
  4. "POLL:FAIL" → 스냅샷 + 이슈 기록 + 다음 Step
  5. 타임아웃 → 스냅샷 + 이슈 기록 + 다음 Step
```

**auto=false (기본)**: 작성 완료 후 `ExitPlanMode`로 사용자 승인을 요청한다.
**auto=true**: `ExitPlanMode`를 호출하지 않고 즉시 Phase 1.5로 진행한다. Plan 내용은 내부적으로 확정된 것으로 간주한다.
