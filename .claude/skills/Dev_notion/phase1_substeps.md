# Phase 1 서브단계: 이미지 분석 + Unity 실사

> `phase1_analysis.md`의 3단계 완료 후, 이 파일의 절차를 수행한다.

---

## 3-1단계: 이미지 다운로드 및 시각 분석 (각 분석가가 수행)

> 이미지 다운로드 실패 시: 1회 재시도 → 실패 시 `[이미지 로드 실패: {URL}]` 태그로 기록하고 분석 계속 진행

각 분석가가 담당 페이지(+ 하위문서)에서 이미지를 식별하고 분석:

1. `<image source="URL">` 태그를 **모두** 수집
2. 스크래치패드에 다운로드: `{스크래치패드}/notion_images/page{N}_{순번}.png`
3. `Read` 도구로 시각 분석 (병렬 호출)
4. 이미지가 없으면 건너뜀

---

## 3-2단계: Unity MCP 실사 분석 (리드가 순차 수행)

> **분석가 프롬프트에서 분리된 별도 단계.** 3단계 + 3-1단계 완료 후, 리드가 각 페이지의 분석 결과를 읽고 관련 씬/프리팹/오브젝트를 순차 조사한다.
> Unity CLI는 WebSocket 단일 커넥션이므로 **순차 실행**한다.

### 실행 조건
- Phase 0에서 Unity CLI 연결이 확인된 경우에만 수행
- CLI 미연결 시 `[Unity 실사 미수행: CLI 미연결]` 태그로 기록, 4단계로 진행

### 조사 항목

각 `_devnotion_page{N}_분석.md`의 "기존 코드 매핑" 섹션에서 언급된 씬/프리팹/UI를 대상으로:

1. **씬/프리팹 구조 확인**: `unity-cli.mjs open_scene` → `get_scene_hierarchy`
2. **기존 컴포넌트 확인**: `unity-cli.mjs find_objects_by_criteria` + `get_component_data`
3. **UI 요소 확인**: `unity-cli.mjs find_ui_elements` + `inspect_ui_layout`
4. **프리팹 내부 확인**: `unity-cli.mjs open_prefab` → 계층 확인 → 프리팹 닫기
5. **스크린샷**: `unity-cli.mjs screenshot` → `Read`로 기획과 비교
6. **에셋 의존성**: `unity-cli.mjs find_references` + `get_asset_dependencies`

### 결과 기록
- 각 `_devnotion_page{N}_분석.md`의 "Unity 실사 분석" 섹션에 추가 기록
