# Phase 0: Mermaid 플로우 로드 + 시나리오 이력 확인

> **Phase 1 시작 전에 반드시 실행한다.**

> ⚠️ CHECKPOINT: Mermaid 블록이 존재하는가? → 없으면 STOP. 추측으로 플로우를 만들지 말 것.

1. `Read` 도구로 `.claude/game_flow_analysis.md` 파일을 읽는다
2. 파일이 존재하면:
   - **Mermaid 블록 파싱**: 파일 내의 모든 ` ```mermaid ` 블록을 추출한다
   - 각 Mermaid 블록에서 **노드 ID 목록**과 **엣지(전환) 목록**을 파싱한다:
     ```
     노드: MAIN_MENU, STORE, COMBAT_BEGIN, COMBAT_ACTION, ...
     엣지: MAIN_MENU -->|"시작 버튼"| STORE, STORE -->|"PendingCombatKey"| COMBAT_BEGIN, ...
     분기: CHECK_END{"승패 판정"} → 승리/패배 경로
     ```
   - 이 구조화된 노드/엣지가 Phase 1의 실행 스크립트 골격이 된다
   - Phase 1의 1~3단계(병렬 에이전트)를 건너뛰고 **4단계(검증 포인트)로 직행**
   - 문서의 "QA 시나리오 제안" 테이블에서 **노드 ID 시퀀스** 기반으로 시나리오 목록을 로드한다
3. 파일이 없거나 Mermaid 블록이 없으면 → **중단 + game_flow_analysis 실행 필수**:
   - **auto=false**: `AskUserQuestion`으로 `/game_flow_analysis` 실행을 요청한다. Frame_QA는 Mermaid 플로우차트 없이 실행할 수 없다.
   - **auto=true**: 자동으로 `/game_flow_analysis`를 내부 실행한다 (game_flow_analysis 스킬의 Stage 1~Phase 5 전체 수행). 완료 후 Phase 0을 다시 실행한다.
   - **자연어 플로우 fallback은 존재하지 않는다.** Mermaid 다이어그램이 유일한 플로우 입력 포맷이다.
4. **시나리오 이력 로드 (auto=true 필수)**:
   - `Read` 도구로 `_qa/history/scenario_log.md` 파일을 읽는다
   - 파일이 존재하면:
     - 각 `## Run` 블록에서 시나리오 이름 리스트를 파싱
     - 최근 3개 Run 내의 시나리오를 `recentlyTested` 목록으로 수집
     - 이번 사이클에서 `recentlyTested`에 포함된 시나리오는 **후순위**로 밀림
   - 파일이 없으면:
     - 모든 시나리오를 미실행으로 간주 (첫 실행과 동일)
   - **시나리오 선택 알고리즘**:
     ```
     candidates = 전체 시나리오 풀
     unused = candidates - recentlyTested    (이전 3회 실행에서 미사용)
     if len(unused) >= 5:
         select 5 from unused (우선순위: Critical > UI > Edge > Perf)
     else:
         select all unused + fill from oldest recentlyTested
     ```
5. `_qa/` 디렉토리 존재 확인 (Glob으로 `_qa/lib/common.lua` 검색)
6. 존재하면:
   - 기존 Lua QA 스크립트를 Read로 로드
   - 이번 시나리오에 해당하는 verify 스크립트가 있으면 Phase 1의 5단계(Lua 스크립트 계획)를 **보강 모드**로 실행
   - 이번 시나리오에 해당하는 verify 스크립트가 없으면 **신규 생성 모드**로 실행
7. 존재하지 않으면:
   - Phase 1.5에서 `_qa/` 디렉토리 구조를 초기 생성
8. `Read` 도구로 `_qa/cache/known_patterns.md` 파일을 읽는다
9. 파일이 존재하면:
   - 이번 시나리오와 관련된 패턴을 식별한다
   - Phase 1의 4단계(검증 포인트 도출)에서 해당 패턴을 **우선 검증 항목**으로 반영한다
   - 특히 "회귀 감시: Yes"인 항목은 verify 스크립트에 반드시 포함한다
   - 관련 패턴이 있으면 Plan에 다음을 명시한다:
     ```
     ### Knowledge Cache 반영
     | 패턴 | 출처 | 이번 시나리오 적용 |
     |------|------|------------------|
     ```
10. 파일이 없으면: 무시하고 진행 (첫 실행과 동일)
11. `_qa/verify/` 디렉토리의 기존 스크립트를 `Read`로 읽고 **성숙도를 판정**한다
12. 판정 기준:

| 성숙도 | 조건 | Phase 1 동작 |
|--------|------|-------------|
| **신규** | 해당 시나리오의 verify 스크립트 없음 | Phase 1 풀분석 (1~6단계 전체) |
| **성장중** | 스크립트 존재, 검증 항목 5개 미만 | Phase 1 경량분석 (에이전트 1개만: 변경된 코드 탐지) |
| **성숙** | 스크립트 존재, 검증 항목 5개 이상 | Phase 1 스킵, 기존 스크립트 + Plan 직행 |

13. 검증 항목 수 계산: 스크립트 내 `qa.check`, `qa.safe_check` 호출 횟수를 센다
14. 판정 결과를 명시한다:
    ```
    ### 스크립트 성숙도 판정
    | 시나리오 | 스크립트 | 항목 수 | 성숙도 | Phase 1 |
    |---------|---------|--------|--------|---------|
    | store_scene | _qa/verify/store_scene.lua | 12 | 성숙 | 스킵 |
    | combat_entry | _qa/verify/combat_entry.lua | 3 | 성장중 | 경량 |
    | reward_flow | (없음) | 0 | 신규 | 풀분석 |
    ```

---

## Phase 0 후반: Mermaid 다이어그램 구조 검증

> **Phase 0 전반 완료 후 반드시 실행한다.** Mermaid 다이어그램이 Frame_QA의 유일한 플로우 입력이다.

### 목적
Mermaid 다이어그램을 **테스트 스크립트의 뼈대로 사용하기 전에 구조적 완전성을 검증**한다.
노드/엣지가 코드 수준으로 명시적이므로, 누락된 경로를 기계적으로 발견할 수 있다.

### 절차

#### 1. 구조 검증 (필수)
파싱된 노드/엣지를 기반으로 다음을 확인한다:

- **고립 노드 탐지**: 인바운드 엣지도 아웃바운드 엣지도 없는 노드 → 플로우에서 빠진 연결
- **막다른 경로 탐지**: 아웃바운드 엣지가 없는 비-종료 노드 → 전환 누락 (FINAL, EXIT 등 종료 노드 제외)
- **분기 완전성**: 분기 노드(중괄호 `{}`)의 아웃바운드 엣지가 모든 경우를 커버하는지 확인
  - 예: `CHECK_END{"승패 판정"}`에 "승리"/"패배" 엣지만 있고 "무승부" 경로가 누락된 것은 아닌지
- **시나리오 경로 커버리지**: QA 시나리오 테이블의 노드 ID 시퀀스가 실제 Mermaid 그래프에서 유효한 경로인지 확인

#### 2. 누락 보완 (검증 실패 시)
고립 노드나 막다른 경로가 발견되면:
- 코드베이스를 탐색하여 누락된 전환 조건을 확인한다 (Grep으로 해당 노드에 대응하는 클래스의 전환 로직 검색)
- `game_flow_analysis.md`의 Mermaid 블록을 Edit으로 보완한다
- 보완 내용을 기록한다:
  ```
  ### Mermaid 보완 사항
  | 보완 유형 | 노드/엣지 | 원인 | 수정 내용 |
  |-----------|----------|------|----------|
  | 막다른 경로 | REWARD_UI | 보상 UI 닫기 전환 누락 | REWARD_UI -->|"닫기"| STORE 추가 |
  ```

#### 3. 노드 ID → Step 매핑 테이블 생성
검증 완료된 Mermaid 노드를 실행 스크립트 Step으로 매핑한다:

```markdown
### 노드 → Step 매핑
| 노드 ID | Step # | 액션 | MCP 도구 | 검증 레벨 |
|---------|--------|------|---------|----------|
| MAIN_MENU | 1 | 시작 버튼 클릭 | click_ui_element | L1 |
| STORE | 2 | 상점 씬 로드 확인 | get_state 폴링 | L1+L4 |
| STORE_NPC_CLICK | 3 | NPC 클릭 | click_ui_element | L2 |
| COMBAT_BEGIN | 4 | 전투 씬 진입 | get_state 폴링 | L1+L2+L4 |
| ... | ... | ... | ... | ... |
```

이 테이블이 **Phase 1 Step 6 (실행 스크립트 작성)**의 직접적인 입력이 된다.
