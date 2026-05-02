# Phase 2: 플레이 모드 — CLI-first 하이브리드 실행

> **auto=false (기본)**: 사용자 승인 후에만 실행한다.
> **auto=true**: Phase 1.5 완료 후 즉시 실행한다. 사용자 승인을 요청하지 않는다.
> **CLI가 기본이다.** 80%의 반복/폴링/검증은 CLI 배치로 실행하고, Claude는 20%의 판단/Vision 분석만 담당한다.
> **플로우 파악, UI 탐색, 다음 단계 판단을 하지 않는다.**

## CLI-first 원칙

```
결과를 미리 알고 있는가?
  ├─ YES → CLI  (스크린샷, 씬 로드 확인, 폴링, Lua 검증, UI 클릭)
  └─ NO  → MCP  (결과를 보고 다음 행동을 결정해야 하는 경우)
```

| 작업 유형 | 실행 주체 | 이유 |
|-----------|----------|------|
| 씬 전환 대기 + 검증 | **CLI** (`qa-scene-entry.mjs`) | 반복 폴링, 판단 불필요 |
| UI 클릭 | **CLI** (`qa-click.mjs`) | 예측 가능한 액션 |
| 조건 폴링 (턴 변경, 상태 대기) | **CLI** (`qa-poll.mjs`) | 토큰 0으로 반복 대기 |
| Lua 검증 실행 | **CLI** (`qa-step.mjs`) | 결과 수집만 |
| 스크린샷 캡처 | **CLI** (runner/step 내장) | 판단 불필요 |
| **Vision 분석** | **Claude** (`Read` 이미지) | 이미지 이해 필요 |
| **실패 원인 분석** | **Claude** (MCP 도구) | 적응적 판단 필요 |
| **에러 복구/우회** | **Claude** (MCP 도구) | 컨텍스트 기반 결정 |

## 실행 흐름 (CLI 기본 → Claude 개입 → MCP 폴백)

### Step 1: CLI 배치 실행 (토큰 0)

Phase 1.5에서 생성한 Plan JSON을 **qa-runner.mjs 한 번으로 전체 실행**:

```bash
node .mcp-server/qa-runner.mjs --plan _qa/plans/scenario_[name].json --timeout 300000
```

- **전체 시나리오가 1 Bash 호출로 완료됨**
- 내부에서 play mode 진입 → step 순차 실행 → screenshot 수집 → play mode 종료
- Claude 턴 소비: **1턴** (Bash 실행)

> **auto=false에서 Step별 세밀한 제어가 필요하면** qa-step.mjs 개별 실행:
> ```bash
> node .mcp-server/qa-step.mjs --step-file _qa/steps/step_01.json
> ```
> 각 Step 결과를 Claude가 확인한 뒤 다음 Step을 결정할 수 있음.

### Step 2: Claude 개입 — 결과 분석 (토큰 사용)

runner/step의 JSON 결과를 파싱하여 **Claude가 판단해야 하는 부분만** 처리:

```
1. JSON 결과 파싱:
   - steps[].action.success → FAIL이면 [EVENTSYSTEM_FAIL] 이슈 기록
   - steps[].poll.timedOut → true이면 타임아웃 이슈 기록
   - steps[].verify.raw → [PASS]/[FAIL]/[ERROR] 카운트, FAIL은 이슈 기록
   - steps[].consoleErrors → 에러 있으면 이슈 기록

2. Vision 분석 (CLI 불가 — Claude 필수):
   - screenshots 배열의 각 경로를 Read로 이미지 읽기
   - Vision 프롬프트 템플릿 적용:
     · 일반 UI 씬 → 템플릿 1
     · 전투 씬 → 템플릿 2
     · 전/후 비교 → 템플릿 3
   - 7개 카테고리별 이상 탐지 (Layout, Text, Asset, Color, Animation, Z-Order, State)
   - 심각도 분류: Critical / Warning / Info

3. 참조 비교 (reference 스크린샷 존재 시):
   - _qa/screenshots/reference/에 동일 시점 스크린샷이 있으면 비교 분석 (템플릿 3)
   - 첫 실행이면 현재 캡처를 reference로 복사
   - Warning 이상 이슈 발견 시 _qa/screenshots/issues/에 보존
```

### Step 3: 실패 복구 (필요 시에만 — MCP 폴백)

**CLI 실행에서 FAIL이 발생한 Step만** Claude가 MCP 도구로 개입한다:

```
실패 유형별 대응:

CONNECTION_FAILED (WebSocket 연결 실패):
  → 보고서에 [BATCH_FALLBACK] 기록
  → ToolSearch로 Unity MCP 도구 로드
  → 기존 MCP 개별 호출 패턴으로 전환 (아래 "MCP 폴백 패턴" 참조)

EVENTSYSTEM_FAIL (UI 클릭 실패):
  → find_ui_elements로 대상 UI 재탐색
  → instanceId 확인 + interactable 상태 점검
  → 실패 원인 분석 후 보고서에 [EVENTSYSTEM_FAIL] 기록
  → 강제 우회(onClick.Invoke)는 하지 않는다 (핵심 원칙 #12)

POLL_TIMEOUT (조건 폴링 타임아웃):
  → MCP screenshot으로 현재 화면 캡처
  → get_console_logs(filter: "error")로 에러 확인
  → 원인 분석 후 이슈 기록 + 다음 Step 진행

VERIFY_FAIL (Lua 검증 실패):
  → FAIL 항목을 이슈로 기록
  → 추가 MCP 조회(get_component_data, get_ui_state)로 원인 확인 가능
  → 다음 Step 진행
```

## onClick.Invoke vs click_ui_element: 부정 예시

> **핵심 원칙 #12의 구체적 적용 사례.**

### ❌ 금지: onClick.Invoke() 직접 호출
```csharp
// 이것은 EventSystem을 우회한다 — QA 신뢰도를 저하시킨다
var runBtn = combatUI.GetType().GetProperty("RunButton").GetValue(combatUI) as UnityEngine.UI.Button;
runBtn.onClick.Invoke();  // ❌ EventSystem 이벤트 체인 우회
```

### ✅ 권장: click_ui_element로 EventSystem 경유
```
find_ui_elements(filter: "Button")  → instanceId 확인
click_ui_element(instanceId: 12345)  → EventSystem 경유 클릭 (Enter→Down→Up→Click)
```

### ❌ 금지: IPointerClickHandler 직접 호출
```csharp
// EventSystem 없이 직접 인터페이스 호출 — 실제 유저 경험과 다른 경로
var handler = target.GetComponent<IPointerClickHandler>();
handler.OnPointerClick(new PointerEventData(EventSystem.current));  // ❌
```

### ✅ 권장: CLI 배치로 클릭
```bash
node .mcp-server/qa-click.mjs --name "Run" --wait 500
```

## MCP 폴백 패턴 (CLI 전체 실패 시)

> CLI가 완전히 사용 불가한 경우에만 적용한다. 보고서에 `[BATCH_FALLBACK]` 태그를 기록한다.

```
1. UI 액션 (EventSystem 경유 필수)
   - 버튼 클릭: find_ui_elements → click_ui_element (EventSystem 경유)
   - 텍스트 입력: set_ui_input
   - 키보드/마우스: simulate_input
   - execute_code로 onClick.Invoke() 등 직접 호출을 하지 않는다

2. 대기 (최소화)
   - wait_for_seconds는 최소값만 사용 (0.5~1초)
   - 조건 폴링: 0.3초 간격, 타임아웃 15초

3. 상태 검증
   - 우선: 전용 MCP 도구 (get_component_data, get_ui_state, find_objects_by_criteria)
   - 차선: Lua 배치 (execute_code → LuaEnv → verify script)
   - 최후: C# 리플렉션 fallback (부록 참조)

4. 스크린샷 & Vision
   - screenshot(view: "game") → Read로 Vision 분석 (동일 프로토콜)

5. 성능 데이터
   - execute_code로 Profiler API 호출 또는 Lua env.fps()/env.memoryMB()
```

## Lua 배치 실행 C# 래퍼 (표준 패턴)

모든 Lua 검증 Step에서 동일한 래퍼를 사용한다.

**주의: LuaEnv 인스턴스 제약**
- 매 Step마다 `new XLua.LuaEnv()`를 생성하므로, 프로젝트의 기존 LuaEnv에 등록된 커스텀 바인딩을 사용할 수 없다.
- `_qa/lib/bootstrap.lua`가 필수 헬퍼를 자체 등록하므로, verify 스크립트에서 `bootstrap.lua`를 로드하여 사용한다.

Phase 1 6단계의 Lua 실행 래퍼 참조.

**Fallback 전략**: Lua 실행 실패 시 → C# 리플렉션 기반 검증으로 대체
1. LUA_ERROR 발생 → 에러 메시지에서 원인 파악
2. XLua `typeof()` 실패 → `bootstrap.find()` (MonoBehaviour 순회 fallback)
3. Lua 전체 실패 → 해당 Step의 검증을 C# execute_code로 대체 (리플렉션 패턴)

**C# 리플렉션 Fallback 패턴** (Lua 실패 시 대체):
```csharp
// 프로젝트 타입은 MonoBehaviour 순회 + GetType().Name 비교로 접근
var allMbs = UnityEngine.Object.FindObjectsByType(typeof(UnityEngine.MonoBehaviour), UnityEngine.FindObjectsSortMode.None);
UnityEngine.MonoBehaviour target = null;
foreach (var mb in allMbs) {
    if (mb.GetType().Name == "TargetTypeName") { target = mb as UnityEngine.MonoBehaviour; break; }
}
// 프로퍼티는 GetProperty().GetValue() 체인으로 접근
var value = target.GetType().GetProperty("PropName").GetValue(target);

// **주의**: System.Collections.Generic, System.Text.StringBuilder,
// System.Reflection.BindingFlags, System.Array 등은 CS0433 중복 오류 발생
// → 문자열 연결(+), GetProperty/GetValue 체인으로 우회
```

## Lua 검증 스크립트 작성 규칙
- `CS.UnityEngine.*`로 Unity API 접근
- `CS.Izakoza.*`로 프로젝트 타입 접근 (reflection 모드)
- `typeof(CS.SomeType)`으로 타입 참조
- 결과는 반드시 `return "문자열"` 형태로 반환
- PASS/FAIL 형식으로 각 검증 결과 포맷

## 핵심 규칙

- **스크립트에 없는 행동을 하지 않는다.** 예상과 다른 상황이 발생하면 이슈로 기록하고 계속 진행한다.
- **플로우 판단을 위해 스크린샷을 분석하지 않는다.** 스크린샷은 오직 가시적 버그 검출 용도이다.
- **instanceId 갱신**: 버튼 클릭 전 반드시 `find_ui_elements`로 새로 가져온다.
- **재시도**: 씬 전환 시 `get_state` 최대 5회 재시도, 각 시도 간 2초 대기
- **전투씬 폴링**: 자동 진행 씬은 3초 간격 상태 폴링
- **타임아웃**: 각 대기에 최대 15초, 초과 시 이슈 기록 후 다음 Step
