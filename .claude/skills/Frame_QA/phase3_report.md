# Phase 3: 결과 보고 & 임시 파일 정리

## 1단계: 플레이 모드 정리
- `node .mcp-server/unity-cli.mjs --eval "..."` 로 `__FrameProfiler__` 오브젝트 제거
- `node .mcp-server/unity-cli.mjs set_play_mode '{"play":false}'` 로 플레이 모드 종료

## 2단계: 임시 데이터 파일 삭제/복원 + _qa/ 결과 기록

> **반드시 실행한다. QA용 임시 파일이 프로젝트에 남으면 안 된다. _qa/ 스크립트는 삭제하지 않는다.**

```
1. scratchpad/_tempQA_registry.json 읽기

2. 신규 생성 파일 삭제 (created 목록)
   - 각 파일 경로에 대해 Bash로 삭제: rm "[경로]"
   - .meta 파일도 함께 삭제: rm "[경로].meta"

3. 백업 파일 복원 (backed_up 목록)
   - 각 파일에 대해:
     - Bash로 .bak 파일을 원본 위치로 이동: mv "[경로].bak" "[경로]"

4. _qa/ 결과 기록 (삭제하지 않음)
   - Write로 _qa/history/last_result.txt에 이번 QA 결과 저장
   - 포맷: 날짜, 시나리오, PASS/FAIL 요약, 발견 이슈

4b. 시나리오 이력 기록 (auto=true, 최종 사이클에서만)
   - 10사이클 완료 후 `_qa/history/scenario_log.md`에 이번 실행 기록을 **append**
   - 파일이 없으면 새로 생성
   - 포맷:
     ```markdown
     ## Run [날짜T시간] ([N] cycles)
     1. [시나리오명1]
     2. [시나리오명2]
     3. [시나리오명3]
     4. [시나리오명4]
     5. [시나리오명5]
     ```
   - Read로 기존 내용을 읽고, 끝에 새 블록을 추가하여 Write
   - 이 파일은 git 추적 가능 (팀원과 QA 이력 공유용)

5. _qa/screenshots/current/ 정리
   - current/ 디렉토리의 스크린샷을 정리 (이슈 발견된 것은 이미 issues/에 복사됨)
   - reference/ 초기 생성 여부를 보고서에 기록

6. _qa/ 스크립트 성장 (발견 사항 반영)
   - QA 중 발견된 새로운 검증 필요 항목이 있으면:
     - 해당 verify 스크립트에 Edit으로 검증 항목 추가
     - 주석: "-- vN: [날짜] [발견 사항] 추가"
   - 없으면 건너뜀

6. AssetDatabase.Refresh() 호출
   - execute_code: UnityEditor.AssetDatabase.Refresh();

7. 임시 레지스트리 삭제 (scratchpad만)
   - scratchpad/_tempQA_registry.json 삭제

8. 삭제/복원 결과를 보고서에 포함
```

**실패 시 안전장치:**
- 삭제/복원 실패 시 보고서에 **Critical** 이슈로 기록
- 실패한 파일 경로와 원인을 명시하여 사용자가 수동 정리 가능하도록 함

## 3단계: 보고서 작성

> **코드를 보여주지 않는다.** 흐름과 결과만 간결하게 보고한다.

```markdown
## Frame QA 결과

### 테스트 흐름
1. [씬A] → [액션] → [씬B] → ... (실제 실행한 흐름을 순서대로 나열)

### 성능
- 평균 FPS: [N] | 최저 FPS: [N] | 스파이크: [N]회 | 메모리: [N] MB

### 발견 이슈
> 이슈가 없으면 "이슈 없음" 으로 표기

| 심각도 | 위치 | 내용 |
|--------|------|------|
| Critical | [씬/Step] | 한 줄 요약 |
| Warning | [씬/Step] | 한 줄 요약 |
| Info | [씬/Step] | 한 줄 요약 |

### 임시 파일 정리
- 임시 데이터 파일: [N]개 삭제 완료
- 백업 복원: [N]개 복원 완료
- 미정리: [없음 / 파일 목록]

### _qa/ 스크립트 상태
- 신규 생성: [파일 목록]
- 보강(검증 추가): [파일 목록 + 추가 항목]
- 기존 유지: [파일 목록]
- 결과 기록: _qa/history/last_result.txt

### 스크립트 성장
| 스크립트 | 이전 항목 | 추가 항목 | 현재 항목 | 성숙도 변화 |
|----------|----------|----------|----------|-----------|

### Vision 분석 결과
> Vision 이슈가 없으면 "VISION_CLEAR (전체 [N]개 스크린샷 분석)" 으로 표기

| 카테고리 | 위치 (씬/UI) | 심각도 | 설명 | 스크린샷 |
|---------|-------------|--------|------|---------|
| Text | 전투 Scene / NPC HP | Critical | "0/0" 초기화 미완 표시 | `issues/combat_npc_hp.png` |
| Asset | StoreScene / NPC 아이콘 | Critical | 핑크 사각형 (Addressable 누락) | `issues/store_npc_icon.png` |

- 참조 비교: [수행/미수행 (reference 유무)]
- reference 갱신: [갱신됨 / 변경없음 / 초기 생성]

### 노드 커버리지
| 전체 노드 | 테스트됨 | PASS | FAIL | 미도달 | 커버리지 |
|----------|---------|------|------|--------|---------|
| [N] | [N] | [N] | [N] | [N] | [N]% |

### 스크린샷
- [이슈가 있는 스크린샷만 첨부, 정상 화면은 생략]
```

심각도 기준: Vision 프로토콜의 "Vision 이슈 심각도 자동 분류 기준" 참조. 추가로 `[EVENTSYSTEM_FAIL]`은 Critical.
