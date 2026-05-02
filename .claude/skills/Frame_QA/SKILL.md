---
name: Frame_QA
description: 게임 QA 자동화 스킬. "QA 돌려줘", "테스트 해줘", "버그 찾아줘", "플레이 검증", "자동 테스트" 등의 요청에 사용합니다. 코드 분석 → 플레이 모드 실행 → 로직/성능/비주얼 통합 검증을 수행합니다. Unity 통신은 Bash에서 node .mcp-server/unity-cli.mjs 및 qa-*.mjs CLI로만 수행 (MCP 도구 호출 금지).
argument-hint: "[auto=true|false] [특정 시나리오 또는 빈칸으로 전체 검사]"
---

## Auto-Cycle 모드 (auto=true)

> **인자에 `auto=true`가 포함되면 이 모드가 활성화된다.**

- 모든 Phase를 사용자 허가 없이 자동 진행 (EnterPlanMode/ExitPlanMode/AskUserQuestion 생략)
- **10회 사이클을 서로 다른 시나리오로 자동 반복**
- 시나리오 선택: `_qa/history/scenario_log.md`에서 이전 이력 참조, 중복 최소화
- 사이클 간 플레이 모드 종료 → 재진입으로 깨끗한 상태 유지
- 상세: `phase4_autocycle.md` 참조

---

## 비파괴 관측 우선 원칙

> 게임 상태를 변경하지 않는 읽기 전용 검증을 최우선으로 한다.

**관측 비용 계층:**

| 비용 | 관측 유형 | Frame_QA 매핑 | 예시 |
|------|----------|-------------|------|
| 낮음 | **읽기 전용** | MCP 조회 도구, Lua 읽기 전용 | `get_component_data`, `get_ui_state`, `find_objects_by_criteria` |
| 중간 | **경미한 영향** | `screenshot`, `get_console_logs` | 스크린샷 캡처 (프레임 부하) |
| 높음 | **상태 변경** | `click_ui_element`, `simulate_input` | UI 조작 (상태 변경 유발) |
| 최대 | **직접 조작** | `execute_code` 상태 변경 | 씬 전환, 데이터 수정 (최후 수단) |

**실행 원칙: 낮은 비용부터 단계적으로 올린다.**
- Step 시작 시 읽기 전용 관측으로 현재 상태 파악
- 필요한 정보가 부족하면 한 단계 올림
- 직접 조작은 Plan에 명시된 경우에만 수행

---

## 핵심 원칙

1. **Mermaid 플로우차트가 유일한 플로우 입력이다.** 자연어 플로우 설명은 사용하지 않는다. `game_flow_analysis.md`의 Mermaid 블록에서 노드 ID와 엣지를 파싱하여 테스트 스크립트의 뼈대로 사용한다. Mermaid가 없으면 `/game_flow_analysis`를 먼저 실행한다.
1-1. **Plan 모드에서 모든 플로우를 완벽히 파악한다.** 플레이 모드에서 플로우를 파악하려 하지 않는다. (**auto=true 시에도 Plan 분석은 수행하되, EnterPlanMode/ExitPlanMode 호출과 사용자 승인만 생략한다.**)
2. **플레이 모드는 스크린샷으로 가시적 버그만 잡는 용도이다.** 플로우 판단, UI 탐색, 다음 단계 결정은 모두 Plan에서 끝낸다.
3. **Plan의 실행 계획은 "스크립트" 수준으로 구체적이어야 한다.** 플레이 모드에서는 Plan에 적힌 순서대로 기계적으로 실행만 한다.
4. **game_flow_analysis 결과가 있으면 활용한다.** `/game_flow_analysis`로 생성된 플로우 문서가 있으면 Phase 1을 간소화하고 해당 문서를 기반으로 실행 스크립트를 작성한다.
5. **병렬 에이전트를 적극 활용한다.** Phase 1의 독립적인 분석 단계는 Explore 에이전트를 병렬 launch하여 수행한다. 에이전트당 `max_turns`를 넉넉히 설정한다.
6. **임시 데이터 파일(XML 등)은 자동 생성/삭제한다.** Lua QA 스크립트는 `_qa/` 디렉토리에 영구 보관한다.
7. **상태 검증은 `_qa/` 디렉토리의 Lua 스크립트로 수행한다.** 스크립트는 QA 반복을 통해 지속 성장하며, 삭제하지 않는다. 기존 스크립트가 있으면 재사용하고 부족분만 추가한다. **스크립트 성숙도(검증 항목 수)에 따라 Phase 1의 코드 분석 범위를 자동 축소하여, QA가 반복될수록 실행 속도가 점진적으로 향상된다.**
8. **Lua 검증은 반드시 pcall로 감싼다.** `safe_check`/`safe_info` 패턴으로 개별 검증 실패가 전체를 중단시키지 않게 한다. XLua `typeof()` 실패, `FindAnyObjectByType` null 반환 등이 빈번하므로 에러 격리가 필수다.
9. **비동기 검증은 "C# 폴링 → Lua 배치 검증" 시퀀스를 따른다.** Lua는 단일 프레임 동기 실행이므로, 비동기 대기(씬 로드, 애니메이션 완료)는 C# 폴링으로, 상태 검증은 Lua로 분리한다.
10. **검증은 4단계 레벨로 구분한다.** Level 1(컴파일), Level 2(런타임 예외), Level 3(로직 정합성), Level 4(Vision 시각 정합성). Plan에서 각 Step마다 적용할 레벨을 명시한다.
11. **QA용 LuaEnv는 프로젝트 바인딩을 사용할 수 없다.** `_qa/lib/bootstrap.lua`로 필수 헬퍼를 자체 등록한다. 프로젝트의 기존 LuaEnv(`luaEnv.Global.Set`으로 등록한 함수)는 접근 불가하므로, CS.* 직접 접근 + bootstrap 헬퍼로 우회한다.
12. **QA는 EventSystem을 통한 실질적 UI 조작을 최우선으로 한다.** `execute_code`를 통한 강제 실행(onClick.Invoke, 내부 메서드 호출 등)은 **지양**한다. 다음 우선순위를 엄격히 따른다:
    1. **EventSystem 기반 UI 조작 (필수 우선)**: `click_ui_element`, `simulate_input`으로 실제 유저와 동일한 EventSystem 경로를 통해 조작한다. 이것이 QA의 기본이자 핵심이다.
    2. **게임 규칙 내 행동**: 카드 배치 → Run 버튼 클릭, 이동 → 공격 범위 확보 등 게임 메카닉을 따름
    3. **상태 검증용 읽기 전용**: `execute_code`/Lua로 내부 상태를 **읽기만** 하여 검증 (PASS/FAIL 판정). 상태 변경은 절대 하지 않는다.
    4. **코드 조작 (최후 수단 — 사실상 금지)**: BootStrap stuck 등 불가피한 경우에만 `execute_code`로 직접 상태 변경. 이 경우 보고서에 "코드 직접 조작" 명시 + 사유 기록
    - **금지 패턴**: `onClick.Invoke()` 직접 호출, HP.Value=0 직접 설정, MakeDead() 호출, 내부 Phase 강제 전환, 비공개 메서드 Invoke, `IPointerClickHandler.OnPointerClick()` 직접 호출
    - **권장 패턴**: `find_ui_elements` → `click_ui_element`로 EventSystem 경유 클릭, 이동 카드로 사거리 확보 → 공격 카드 배치 → Run 클릭 → 턴 진행 → 자연스러운 승리/패배
    - **EventSystem 실패 시 대응**: `click_ui_element`가 실패하면 **즉시 원인을 분석**하고 보고서에 다음을 기록한다:
      - 실패한 UI 요소의 이름, 경로, instanceId
      - EventSystem 상태 (존재 여부, 개수, Raycast 차단 여부)
      - 실패 원인 추정 (Canvas 비활성, Raycast Target 미설정, 다른 UI에 의한 가림, CanvasGroup interactable=false 등)
      - `[EVENTSYSTEM_FAIL]` 태그로 보고서에 명시
    - **근거**: `execute_code`로 강제 실행하면 EventSystem 이벤트 체인이 우회되어 실제 유저 경험과 다른 경로가 실행된다. 또한 코드 조작은 이벤트 체인 부작용(HP 변경 → OnValueChange → 연쇄 사망), 상태 불일치(IsDead 플래그 미갱신), 프로덕션과 다른 경로 실행 등 QA 신뢰도를 저하시킨다. EventSystem 실패 자체가 중요한 버그 징후이므로, 강제 우회보다 실패 원인 기록이 더 가치 있다.
13. **크로스-시나리오 패턴을 캐싱한다.** 10사이클 종합 보고서 출력 후, Critical/Major 이슈를 `_qa/cache/known_patterns.md`에 추상화된 규칙으로 기록한다. Phase 0에서 이 파일을 읽어 검증 포인트에 반영하여, 이전 QA에서 발견된 패턴의 재발과 유사 구조 결함을 우선 탐지한다.
14. **CLI-first 하이브리드를 따른다.** Phase 2의 80%는 CLI 배치(`qa-runner.mjs`)로 실행하고, Claude는 Vision 분석(스크린샷 이미지 이해)과 실패 복구(적응적 판단)에만 개입한다. "결과를 미리 알고 있는가?" → YES면 CLI, NO면 MCP. CLI 배치는 토큰을 소비하지 않으며 MCP 대비 3-5배 빠르다.

---

## Phase 라우팅

각 Phase 진입 전, `Read` 도구로 이 디렉토리의 해당 서브파일을 로드한다.
**한 번에 하나의 서브파일만 로드한다. 전부 읽지 말 것.**

| Phase | 서브파일 | 트리거 |
|-------|---------|--------|
| 0 | `mermaid_and_scenario.md` | 항상 최초 |
| 1 | `phase1_plan.md` | Phase 0 완료 후 |
| 1.5 | `phase15_prep.md` | Phase 1 완료 후 |
| 2 | `phase2_execution.md` | Phase 1.5 완료 후 |
| 3 | `phase3_report.md` | Phase 2 완료 후 |
| 4 | `phase4_autocycle.md` | auto=true일 때만 |
| Vision | `vision_protocol.md` | 스크린샷 분석 시 |
| 도구 상세 | `tool_reference.md` | 도구 파라미터 확인 시 |
| 부록 | `appendix_patterns.md` | 알려진 이슈 발생 시 |

---

## Unity 도구 레퍼런스 (요약)

> 상세: `tool_reference.md` 참조
> **CLI 호출**: `node .mcp-server/unity-cli.mjs [도구명] '[JSON]'`
> **MCP 폴백**: CLI 실패 시에만 `mcp__unity__[도구명]` 사용 + `[BATCH_FALLBACK]` 기록

| 도구 | 용도 | CLI 예시 |
|------|------|---------|
| `click_ui_element` | EventSystem 경유 UI 클릭 | `unity-cli.mjs click_ui_element '{"gameObjectPath":"..."}'` |
| `set_play_mode` | Play Mode 진입/탈출 | `unity-cli.mjs set_play_mode '{"play":true}'` |
| `get_state` | isPlaying, 씬 확인 | `unity-cli.mjs get_state` |
| `screenshot` | Game View 캡처 | `unity-cli.mjs screenshot '{"view":"game"}'` |
| `execute_code` | C# 코드 실행 | `unity-cli.mjs --eval "return ...;"` |
| `get_console_logs` | Console 로그 수집 | `unity-cli.mjs get_console_logs '{"filter":"error"}'` |
| 기타 14개 도구 | 상세 참조 | `tool_reference.md` |

---

## CLI 배치 도구 레퍼런스 (요약)

> 상세: `tool_reference.md` 참조

| 스크립트 | 용도 | 토큰 |
|----------|------|------|
| `qa-scene-entry.mjs` | 씬 진입 + 4병렬 검증 | 0 |
| `qa-poll.mjs` | 비동기 조건 폴링 | 0 |
| `qa-click.mjs` | UI 클릭 배치 | 0 |
| `qa-step.mjs` | Step 통합 실행 | 0 |
| `qa-runner.mjs` | 전체 시나리오 실행 | 0 |

---

## CLI vs MCP 선택 기준

| 상황 | 사용 도구 | 토큰 |
|------|----------|------|
| 씬 전환 + 검증 | **CLI** `qa-scene-entry.mjs` | 0 |
| 조건 대기 | **CLI** `qa-poll.mjs` | 0 |
| 버튼/UI 클릭 | **CLI** `qa-click.mjs` | 0 |
| Step 실행 | **CLI** `qa-step.mjs` | 0 |
| auto=true 전체 | **CLI** `qa-runner.mjs` | 0 |
| Vision 분석 | **Claude** `Read` 이미지 | ~500 |
| 실패 원인 분석 | **Claude** MCP 도구 | ~200/회 |
| CLI 실패 시 | **MCP 폴백** + `[BATCH_FALLBACK]` | ~200/Step |

**판단 기준**: "결과를 미리 알고 있는가?" → YES면 CLI, NO면 MCP
