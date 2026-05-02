# 부록: MCP 도구 활용 패턴 및 주의사항

실제 QA 실행 과정에서 반복적으로 발생한 문제와 해결 패턴을 정리한다.
**Plan 작성 시 이 제약을 반영하여 실행 스크립트를 작성해야 한다.**

> ⚠️ VERIFY: execute_code에서 System.* 타입 사용 시 CS0433 발생. 추측하지 말 것.

---

## 0. com.jaewon.mcp-unity vs Union MCP (is.nurture.mcp) 차이

> **중요**: 이 프로젝트에는 두 개의 MCP 패키지가 공존한다.
> Frame_QA는 **`com.jaewon.mcp-unity`**의 도구를 사용한다.

| 항목 | com.jaewon.mcp-unity | Union MCP (is.nurture.mcp) |
|------|---------------------|---------------------------|
| `execute_code` Play Mode | **동작함** (EnsureNotPlaying 미호출) | **강제 종료** (EnsureNotPlaying 호출) |
| `screenshot` Game View | **`view: "game"` 지원** | Scene View만 (카메라 경로 수동 지정 필요) |
| UI 조작 도구 | click_ui_element, simulate_input, set_ui_input | 없음 |
| UI 검증 도구 | inspect_ui_layout, get_ui_state, find_ui_elements | 없음 |
| 오브젝트 검색 | find_objects_by_criteria, get_component_data | get_type_info만 |
| Play Mode 제어 | set_play_mode | 없음 |

## 0-1. execute_code가 Play Mode에서 동작하는 이유

`com.jaewon.mcp-unity`의 `ExecuteCodeHandler`는 코드를 받아 즉시 컴파일+실행한다.
Union MCP처럼 `EditorApplication.ExitPlaymode()`를 호출하지 않으므로, **Play Mode 상태에서 게임 오브젝트, 컴포넌트, 런타임 상태에 접근 가능**하다.

**따라서**: C# 폴링 패턴, Lua 래퍼 실행, 런타임 상태 읽기가 모두 Play Mode에서 정상 동작한다.

---

## 1. execute_code 컴파일 제약

> → **Lua 사용 시 해당 없음**: Lua에서 CS.* 직접 접근하므로 C# 컴파일 오류가 발생하지 않음

### System.* 타입 중복 오류 (CS0433)
- `System.Threading.Thread`, `System.AppDomain`, `System.Type` 등 System 네임스페이스 타입 사용 시 CS0433 중복 정의 오류 발생
- **해결**: System 네임스페이스 타입 사용을 피하고 Unity API만 사용
- **예시**: `System.Type` 대신 리플렉션 없이 직접 캐스팅

### 프로젝트 타입 직접 참조 불가
- `Izakoza.Combat.CombatSceneBootstrap` 등 프로젝트 네임스페이스 타입을 execute_code에서 직접 참조하면 "type not found" 오류
- **해결**: `FindAnyObjectByType` 대신 `AssetDatabase.FindAssets("ClassName t:MonoScript")` + GUID 기반 접근
- **예시**:
  ```csharp
  var guids = UnityEditor.AssetDatabase.FindAssets("CombatSceneBootstrap t:MonoScript");
  var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
  var script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(path);
  var type = script.GetClass();
  ```

### Count는 메서드 그룹일 수 있음
- `+ someCollection.Count`는 메서드 그룹 오류 발생 가능
- **해결**: `.Count.ToString()` 또는 `$"{collection.Count}"` 보간 사용

### BindingFlags 사용 불가
- `System.Reflection.BindingFlags` → CS0433.
- **해결**: `GetFields()`/`GetProperties()` 매개변수 없이 호출, 또는 `GetProperty("Name")`으로 이름 직접 지정.

### System.IO.File CS0433
- `System.IO.File.ReadAllText()` → CS0433.
- **해결**: Lua verify 실행 불가 시 C# 리플렉션으로 직접 검증 (부록 7의 전투 시스템 API 참조).

---

## 2. 한국어 경로 처리

> → **Lua 대안**: Lua에서 `CS.UnityEditor.AssetDatabase.FindAssets` 호출로 동일 해결

- `AssetDatabase.LoadAssetAtPath("Assets/Scripts/Izakoza/전투/...")` → null 반환
- **해결**: 직접 경로 지정 대신 `FindAssets`로 이름 검색 후 GUID → 경로 변환
- Plan 작성 시 한국어 폴더가 포함된 에셋은 반드시 FindAssets 방식으로 접근

---

## 3. 씬 전환 패턴

### PersistentMonoSingleton은 씬 전환을 보장하지 않음
- `PersistentMonoSingleton`이라도 **자식 오브젝트**이면 `DontDestroyOnLoad`가 적용되지 않아 씬 전환 시 파괴됨
- **해결**: 씬 전환 후 데이터 전달은 `PlayerPrefs` 등 씬 독립적 수단 사용
- **패턴**: 발신 씬에서 `PlayerPrefs.SetString(key, value)` → 수신 씬의 Bootstrap 컴포넌트에서 `Start()`에 읽기

### 씬 로드 폴링
- 씬 전환 후 MCP 연결이 일시 끊길 수 있음
- **해결**: `get_state` 최대 5회 재시도, 각 시도 간 2초 대기

---

## 4. UI 요소 클릭 패턴

### 스크롤뷰 템플릿/클론 겹침
- ScrollView 내 리스트 아이템이 템플릿 오브젝트와 첫 번째 클론이 동일 y좌표에 위치
- `find_ui_elements`로 두 버튼이 같은 위치에 나오면 **instanceId가 더 큰 것(클론)**을 클릭
- **판별법**: 이름에 `(Clone)` 포함 여부 또는 instanceId 크기 비교

### EventSystem 기반 클릭 실패 시 대응
핵심 원칙 12 참조. `[EVENTSYSTEM_FAIL]` 이슈로 기록하고 강제 우회하지 않는다.

---

## 5. 스크립트 생성 후 컴파일 타이밍

- 새 .cs 파일 생성 후 즉시 `GetClass()` 호출 시 null 반환
- **해결**: `AssetDatabase.Refresh()` 호출 후 재시도 (Unity 컴파일 완료 대기)
- 플레이 모드 중 스크립트 수정 시 도메인 리로드로 인해 BootStrap 씬에서 멈출 수 있음
- **해결**: 플레이 모드 종료 → 컴파일 완료 대기 → 재진입

---

## 6. 고객/랜덤 선택 대응

- 고객 3명이 랜덤 선택되므로 특정 고객이 항상 등장하지 않음
- **해결**: 테스트하려는 인카운터(Battle 등)를 여러 고객에 추가하여 어떤 조합이든 테스트 가능하도록 데이터 준비
- Plan 단계에서 Guest.xml의 EncounterList를 확인하고 필요한 데이터를 사전 추가

---

## 7. 전투 시스템 API 참조

실행 스크립트 작성 시 사용할 수 있는 검증된 API:

```csharp
// 전투 상태 확인
var director = UnityEngine.Object.FindAnyObjectByType<Izakoza.Combat.CombatDirector>();
var instance = director.GetType().GetProperty("Instance")?.GetValue(director);

// 엔티티 목록
var entities = UnityEngine.Object.FindObjectsByType<Izakoza.Combat.Entity.CombatEntity>(
    UnityEngine.FindObjectsSortMode.None);

// UI 확인
var combatUI = UnityEngine.Object.FindAnyObjectByType<Izakoza.Combat.UI.CombatUI>();
var handUI = UnityEngine.Object.FindAnyObjectByType<Izakoza.Combat.UI.HandUI>();
var orderChainUI = UnityEngine.Object.FindAnyObjectByType<Izakoza.Combat.UI.OrderChainUI>();

// 성능 데이터
var fps = 1.0f / UnityEngine.Time.deltaTime;
var memory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
```

**Lua 버전:**
```lua
-- 전투 엔티티 찾기
local entities = CS.UnityEngine.Object.FindObjectsByType(
    typeof(CS.Izakoza.Combat.Entity.CombatEntity),
    CS.UnityEngine.FindObjectsSortMode.None)

-- UI 확인
local combatUI = CS.UnityEngine.Object.FindAnyObjectByType(typeof(CS.Izakoza.Combat.UI.CombatUI))
local handUI = CS.UnityEngine.Object.FindAnyObjectByType(typeof(CS.Izakoza.Combat.UI.HandUI))

-- 성능 데이터
local fps = 1.0 / CS.UnityEngine.Time.deltaTime
local mem = CS.UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)
```

---

## 8. 병렬 에이전트 launch 패턴

```
# Phase 1: 하나의 메시지에서 세 Task를 동시에 호출 (max_turns 넉넉하게)
Task(subagent_type=Explore, max_turns=30, prompt="1단계: 씬 및 게임 시스템 파악 ...", description="씬/시스템 분석")
Task(subagent_type=Explore, max_turns=30, prompt="2단계: 게임 매니저/상태 머신 분석 ...", description="매니저 분석")
Task(subagent_type=Explore, max_turns=30, prompt="3단계: UI 플로우 + UI 레이어 + 데이터 파일 분석 ...", description="UI/데이터 분석")

# 세 에이전트 결과 수신 후 4~6단계 순차 수행
```

---

## 9. Lua QA 배치 검증 패턴 (_qa/ 기반)

### _qa/ 디렉토리 구조
```
프로젝트루트/
  _qa/
    lib/
      common.lua              -- check(), safe_check(), info(), safe_info() 공통 유틸
      bootstrap.lua           -- QA용 LuaEnv 헬퍼 (find, get, sceneName, fps, memoryMB)
      ui_verify.lua           -- Canvas/EventSystem 검증 함수
      performance.lua         -- FPS, 메모리 수집 함수
    verify/
      store_scene.lua         -- 상점씬 상태 검증
      combat_entry.lua        -- 전투 진입 검증
      combat_turn1.lua        -- 전투 턴1 종료 검증
      combat_state.lua        -- 전투 중 상태 검증
      main_menu.lua           -- 메인메뉴 검증
    cache/
      known_patterns.md       -- 크로스-시나리오 주의 패턴 (Phase 0 로드, 종합 보고서 후 기록)
    screenshots/
      reference/              -- 정상 상태 기준 스크린샷 (첫 실행 시 생성, 수동 승인 후 유지)
      current/                -- 현재 실행 캡처 (Phase 3에서 정리)
      issues/                 -- 이상 발견 시 보존 ({날짜}_{시나리오}_{step}.png)
    history/
      last_result.txt         -- 마지막 실행 결과
      scenario_log.md         -- 크로스-런 시나리오 이력 (auto=true 실행마다 append)
      YYYY-MM-DD_[시나리오].md -- 날짜별 QA 리포트
```

- `_qa/`는 Assets 외부 → Unity 컴파일에 영향 없음
- `.gitignore`에 `_qa/history/` 추가 권장 (결과 파일은 로컬용)
- `_qa/lib/`, `_qa/verify/`는 git 추적 가능 (팀 공유 가능)

**참고**: Lua 스크립트 코드는 Phase 1.5, C# Lua 래퍼는 Phase 2의 표준 패턴 섹션 참조. C# 직접 호출이 필요한 경우: UI 클릭 시뮬레이션, AssetDatabase 조작, 조건 폴링(Lua 단일 실행 한계). verify 스크립트는 QA 반복으로 검증 항목이 누적되며, 각 추가 항목에 `-- vN: [날짜] [사유]` 주석을 붙인다.

---

## 10. BootStrap 씬 stuck 대응 패턴

Play Mode 진입 시 BootStrap 씬에서 10초 이상 머무르는 현상이 빈번(3/5 cycle 재현).

### 감지 및 워크어라운드
```
1. set_play_mode(true) 후 wait_for_seconds(5)
2. get_state로 activeScene 확인
3. "BootStrap"이면 wait_for_seconds(10) 후 재확인
4. 여전히 "BootStrap"이면 수동 전환:
   execute_code: UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene2");
5. wait_for_seconds(3) 후 정상 진행
```

### 수동 전환 시 부작용
- `PlayerDataManager`가 DontDestroyOnLoad에 등록되지 않아 Destroy 경고 발생 가능
- `RewardService` 등 BootStrap에서 초기화되는 DontDestroyOnLoad 싱글턴이 생성되지 않음
  - RewardService.Instance 접근 시 프리팹 없이 빈 GameObject로 생성 → UI 참조 모두 null
- **대응**: 보상 UI 등 BootStrap 의존 기능 테스트 시 정상 경로 재현 불가로 **코드 레벨 분석**으로 대체
- **기록**: 보고서에 "BootStrap stuck으로 수동 전환" 명시

---

## 11. EncounterListPresenter Hide() 후 재클릭 문제

### 문제
`EncounterListPresenter.Hide()`를 execute_code로 직접 호출하면 encounter list가 비활성화되나,
이후 NPC 재클릭 시 encounter list가 표시되지 않음.

### 원인
- `Hide()`는 gameObject를 비활성화하지만, `CustomerClickCameraController`의 포커스 상태는 유지됨
- 재클릭 시 카메라가 이미 포커스 상태이므로 encounter list 표시 로직이 스킵됨

### 해결 패턴
```
1. 먼저 CustomerClickCameraController.ReturnToOriginalState() 호출
2. wait_for_seconds(1.5) — 카메라 복귀 애니메이션 대기
3. 그 다음 NPC 재클릭
```

```csharp
// ReturnToOriginalState 호출 패턴
for (int i = 0; i < allMbs.Length; i++) {
    var mb = allMbs[i] as UnityEngine.MonoBehaviour;
    if (mb != null && mb.GetType().Name == "CustomerClickCameraController") {
        mb.GetType().GetMethod("ReturnToOriginalState").Invoke(mb, null);
        break;
    }
}
```

**주의**: `EncounterListPresenter.Hide()`를 직접 호출하지 말 것. 배경 이미지 클릭 또는 `ReturnToOriginalState()`를 통해 정상 UI 닫기 흐름을 사용.

---

## 14. 전투 승리: 자연스러운 플레이 우선 (원칙 #12 적용)

### 문제 (코드 조작 방식의 위험성)
NPC HP를 0으로 직접 설정하거나 ApplyDamage(9999)를 호출하면:
- HP.Value 변경 → OnValueChange 이벤트 → 연쇄 사망(PC까지 사망하는 부작용 확인됨)
- IsDead 플래그 불일치 → CheckEnd 로직이 Win이 아닌 Lose를 반환
- MakeDead() 호출 시 InvocationTargetException 발생
- 프로덕션과 다른 코드 경로 실행 → QA 결과 신뢰도 저하

### 권장 패턴: 자연스러운 전투 진행
```
1. 보드 좌표 확인: execute_code로 PC/NPC 타일 위치 읽기 (읽기 전용)
2. 사거리 분석: XML 카드 데이터에서 공격 범위 확인
3. 이동 카드 배치: click_ui_element로 이동 카드를 핸드에서 선택 → 체인 슬롯에 배치
4. 공격 카드 배치: 사거리 도달 후 공격 카드를 체인에 추가 배치
5. Run 버튼 클릭: click_ui_element로 Run 버튼 클릭
6. 턴 결과 폴링: execute_code로 Turn.Base, Phase, CheckEnd 결과를 읽기
7. 반복: 적이 사망할 때까지 3~6 반복
```

### 코드 조작이 허용되는 예외 상황
- BootStrap stuck으로 수동 씬 전환이 필요한 경우
- 특정 게임 상태 재현이 자연 플레이로 10턴 이상 소요되는 경우 (이 경우에도 보고서에 명시)

### 레거시 코드 조작 패턴 (최후 수단으로만 사용, 보고서에 "코드 직접 조작" 명시)
```csharp
// HP를 1로 약화 (0이 아닌 1로 — 0은 연쇄 이벤트 발생)
valProp.SetValue(npcHp, 1);

// Run 버튼 클릭으로 턴 실행 (UI 클릭 불가 시에만)
var runBtn = combatUI.GetType().GetProperty("RunButton").GetValue(combatUI) as UnityEngine.UI.Button;
runBtn.onClick.Invoke();
```

---

## 15. DontDestroyOnLoad 싱글턴 의존성 확인

### BootStrap 경유 필수 싱글턴 목록
| 싱글턴 | 역할 | 미생성 시 영향 |
|--------|------|-------------|
| RewardService | 보상 처리 + UI | ProcessRewards 호출 시 UI null → 보상 표시 불가 |
| AudioManager | 오디오 재생 | 전투 효과음/BGM 재생 불가 |
| PlayerDataManager | 플레이어 데이터 | 전투 키, 인카운터 데이터 접근 불가 |

### 대응 전략
- **보상/오디오 테스트**: BootStrap stuck 시 코드 레벨 분석으로 대체, 보고서에 N/A 명시
- **전투 진입 테스트**: 수동 전환 후 PlayerDataManager가 존재하면 PendingCombatKey 설정 가능
- **씬 전환 성공 시**: BootStrap 정상 통과하면 모든 싱글턴 사용 가능

---

## 16. 임시 XML 파일 생성/삭제 패턴

```csharp
// XML 파일 생성 예시 (execute_code에서)
var xmlContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Root>
  <Guest Key=""TEST_GUEST"">
    <Name>테스트 손님</Name>
    <EncounterList>TestEncounter</EncounterList>
  </Guest>
</Root>";
System.IO.File.WriteAllText(
    UnityEngine.Application.dataPath + "/Data/TestGuest.xml",
    xmlContent);
UnityEditor.AssetDatabase.Refresh();
```

```csharp
// 파일 삭제 예시 (Phase 3에서)
var path = UnityEngine.Application.dataPath + "/Data/TestGuest.xml";
if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
var metaPath = path + ".meta";
if (System.IO.File.Exists(metaPath)) System.IO.File.Delete(metaPath);
UnityEditor.AssetDatabase.Refresh();
```
