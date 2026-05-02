# Phase 3 + 4: 상세 계획 수립 + 코드 구현

---

## Phase 3: 상세 계획 수립

1. **Validator spawn**: `validator` 에이전트 생성
2. **분석가들 (병렬)**: 각자 담당 페이지 범위의 구현 단위에 대해 상세 계획 작성 (파일/클래스/메서드 수준)
3. **Validator**: 검증 체크리스트 준비 (컴파일, null 참조, MonoBehaviour, Addressables)
4. **리드**: 전체 종합 후 `ExitPlanMode`로 사용자 승인
5. **리드**: `TaskCreate`로 Phase 4 태스크 목록 생성 + `blockedBy` 설정 (분석가 → Builder 전환 배분)

### Phase 3 완료 → 노션 기입

---

## Phase 4: 코드 구현 (Builder/Validator 프로토콜)

### 구현 흐름

1. **분석가 → Builder 전환**: 각 분석가가 자신의 담당 범위의 모듈을 구현 → 완성 시 `[DONE] {파일경로} - {구현 내용 요약}` 신호
   - 미완성 상태에서 완료 신호 금지
   - 중간 결과를 `TaskUpdate description`에 기록
   - 공통 수정 파일이 있는 경우, Phase 2에서 결정된 순서에 따라 순차 구현
2. **Validator**: `[DONE]` 수신 시에만 검증 실행
   - 컴파일 확인 (`ToolSearch: "+ide diagnostics"` → `mcp__ide__getDiagnostics`)
   - 코드 파괴 방지 체크리스트 (아래 참조)
   - 성공: `[PASS] {파일경로}`
   - 실패: `[FAIL] {파일경로} - {에러 내용} - {위치}`
   - **Builder에게 직접 피드백** (리드 경유 불필요)

### 자율 복구 체인 (cross-verify 적용)

컴파일 에러 발생 시, Builder가 사용자에게 묻기 전에 자율 복구를 시도한다:

```
컴파일 에러 발생
  │
  ├─ [복구 1] 에러 메시지 분석 → using 누락/타입 불일치/접근제한자 자동 수정
  ├─ [복구 2] 기존 코드 패턴에서 올바른 API 확인 → 수정
  └─ 2회 실패 → Validator가 리드에게 보고 → 리드가 사용자에게 보고
```

최대 3회 시도 후에도 에러가 있으면 사용자에게 보고합니다.

### 코드 파괴 방지 체크리스트 (cross-verify 적용)

Validator가 [DONE] 검증 시 아래 항목을 필수 점검:

- [ ] 기존 메서드 시그니처를 변경하지 않았는가?
- [ ] Unity 오브젝트 null 체크를 추가했는가?
- [ ] Addressable 키가 실제 존재하는가?
- [ ] SerializeField 추가 시 프리팹에서 할당이 필요한가?
- [ ] UI 오브젝트를 코드로 동적 생성하지 않았는가? (프리팹 기반 필수)
- [ ] 기존 이벤트 구독/해제 흐름을 깨뜨리지 않았는가?
- [ ] MonoBehaviour 라이프사이클 순서를 준수하는가?

### 부정 예시 / 긍정 예시

#### Unity 오브젝트 null 체크

```csharp
// ❌ 잘못된 예시: Unity 오브젝트 null 체크 생략
void UpdateUI()
{
    _label.text = _data.Name;           // _label이 Destroy되었을 수 있음
    _icon.sprite = LoadSprite(_data.Key); // _icon이 null일 수 있음
}

// ✅ 올바른 예시: if (obj != null) 항상 선행
void UpdateUI()
{
    if (_label != null)
        _label.text = _data.Name;
    if (_icon != null)
        _icon.sprite = LoadSprite(_data.Key);
}
```

#### Addressable 로드 타이밍

```csharp
// ❌ 잘못된 예시: Addressable 로드 완료 전에 결과 사용
void Start()
{
    var handle = Addressables.LoadAssetAsync<Sprite>("icon_key");
    _icon.sprite = handle.Result; // 아직 로드 안 됨!
}

// ✅ 올바른 예시: 콜백 또는 await로 로드 완료 보장
async void Start()
{
    var handle = Addressables.LoadAssetAsync<Sprite>("icon_key");
    var sprite = await handle.Task;
    if (_icon != null)
        _icon.sprite = sprite;
}
```

#### MonoBehaviour 라이프사이클 순서

```csharp
// ❌ 잘못된 예시: Start에서 다른 오브젝트의 Awake 결과에 의존
void Start()
{
    var manager = FindFirstObjectByType<GameManager>();
    _data = manager.GetData(); // GameManager.Awake가 아직 안 불렸을 수 있음
}

// ✅ 올바른 예시: 의존 대상의 초기화 시점을 명확히 파악
void Start()
{
    var manager = FindFirstObjectByType<GameManager>();
    if (manager != null && manager.IsInitialized)
        _data = manager.GetData();
}
```

### Phase 4 완료 → 노션 기입
