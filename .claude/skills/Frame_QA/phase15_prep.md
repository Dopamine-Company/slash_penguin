# Phase 1.5: 임시 데이터 파일 생성 + _qa/ 스크립트 준비

> **Phase 2 진입 직전에 실행한다. Plan에서 수립한 임시 데이터 파일 계획과 _qa/ 스크립트 계획을 실행한다.**

## 절차 A: 임시 데이터 파일 (기존과 동일 — 생성 후 Phase 3에서 삭제)

```
1. 임시 파일 레지스트리 초기화
   - _tempQA_created = [] (신규 생성 파일 경로 목록)
   - _tempQA_backed_up = [] (백업된 기존 파일 경로 목록)

2. 기존 파일 백업 (수정이 필요한 경우)
   - Read로 원본 파일 읽기
   - Write로 원본을 [파일명].bak으로 복사
   - _tempQA_backed_up에 경로 추가

3. 임시 파일 생성/수정
   - Plan의 "파일 내용" 섹션에 명시된 내용을 Write로 생성
   - 신규 파일은 _tempQA_created에 경로 추가
   - 기존 파일 수정은 Plan 내용대로 Write로 덮어쓰기

4. .meta 파일 처리
   - Unity에서 새 에셋 파일 생성 시 .meta 파일이 자동 생성됨
   - AssetDatabase.Refresh()를 플레이 모드 진입 전에 호출하여 Unity가 인식하도록 함

5. 생성된 파일 목록을 scratchpad에 기록
   - Write로 scratchpad/_tempQA_registry.json에 저장:
     { "created": [...], "backed_up": [...] }
```

## 절차 B: _qa/ Lua 스크립트 준비 (영구 — 삭제하지 않음)

```
1. _qa/ 디렉토리 존재 확인
   - 없으면: 디렉토리 구조 생성 (_qa/lib/, _qa/verify/, _qa/history/, _qa/cache/, _qa/screenshots/reference/, _qa/screenshots/current/, _qa/screenshots/issues/, _qa/poll/, _qa/steps/, _qa/plans/)
   - 있으면: 기존 구조 유지 (poll/, steps/, plans/ 없으면 추가 생성)

2. lib/ 공통 스크립트 확인
   - _qa/lib/common.lua 없으면 → Plan의 표준 내용으로 Write
   - _qa/lib/bootstrap.lua 없으면 → 표준 내용으로 Write
   - _qa/lib/combat_checks.lua — 전투 시나리오에 필요하면 확인
   - 있으면 → 건너뜀 (기존 유지)

3. verify/ 시나리오 스크립트 처리
   - **신규 모드**: Plan의 Lua 내용을 Write로 생성
   - **보강 모드**: Read로 기존 파일 읽기 → Plan의 추가 검증 항목을
     기존 파일 끝에 Edit으로 추가 (주석으로 "-- vN: [날짜] 추가" 표시)
   - **공유 함수 사용 필수**: `qa.check_event_system_count()`, `qa.collect_perf_data(env)`, `env.getPath()` 사용
   - **전투 스크립트**: `combat_checks.lua` import + `combat.full_combat_check(qa, env)` 사용
```

## 절차 C: CLI 배치용 Step JSON + Poll 코드 생성 (신규)

> **Phase 2에서 CLI 배치 스크립트가 읽을 입력 파일을 생성한다.**

```
1. C# 폴링 코드 파일 생성 (_qa/poll/)
   - 각 비동기 Step의 폴링 조건을 개별 .cs 파일로 생성
   - 파일명: _qa/poll/{step_id}_{설명}.cs
   - 내용: "POLL:OK|POLL:WAIT|POLL:FAIL" 반환하는 C# 코드
   - Windows 셸 이스케이프 문제를 방지하기 위해 반드시 파일로 저장

2. Step JSON 파일 생성 (_qa/steps/)
   - Phase 1의 실행 스크립트 각 Step을 JSON으로 변환
   - 파일명: _qa/steps/step_{N:02d}.json
   - 포맷:
     {
       "id": N, "node": "MERMAID_NODE_ID",
       "action": { "type": "click|input|wait|play", "target": {...}, "wait": 500 },
       "poll": { "codeFile": "_qa/poll/check_xxx.cs", "expect": "POLL:OK", "timeout": 15000 },
       "verify": { "lua": "_qa/verify/scenario.lua" },
       "screenshot": true, "checkErrors": true
     }

3. 시나리오 Plan JSON 생성 (_qa/plans/)  — auto=true 시 qa-runner.mjs 입력용
   - 파일명: _qa/plans/scenario_{name}.json
   - 포맷:
     {
       "scenario": "name",
       "playMode": true,
       "steps": [ ...step 배열 (위 Step JSON과 동일 포맷)... ]
     }
   - 모든 Step을 하나의 배열로 합침
```

## 코드 패턴

```csharp
// Phase 1.5 마지막에 실행 — Unity가 새 파일을 인식하도록
UnityEditor.AssetDatabase.Refresh();
```
