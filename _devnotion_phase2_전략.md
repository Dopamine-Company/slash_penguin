# Phase 2 전략 — slash_penguin

## 흰박스 결정사항
- 펭귄 = Capsule primitive
- 좌/우 볼기짝 = Sphere primitive (각각 독립 오브젝트)
- 팬티 = 납작한 Cube primitive
- 똥 화면 덮기 = UI Image (FullScreen)

## 구현 단위 분해

| # | 단위 | 설명 | 의존성 유형 |
|---|------|------|------------|
| U1 | PenguinGameSettings | ScriptableObject 튜닝 데이터 | 완전 독립 |
| U2 | GameState enum | 상태 열거형 | 완전 독립 |
| U3 | SwipeInputController | 터치/스와이프 감지 | 완전 독립 |
| U4 | GameStateMachine | 상태 전환 관리 | U1, U2 |
| U5 | PantyController | 팬티 벗기기 (스와이프 Y 매핑) | U3, U4 |
| U6 | ButtockController | 볼기짝 활성화/팽창/타격 판정 | U3, U4 |
| U7 | RednessController | redness 상태 + 색상 변화 (DOTween) | U1, U6 |
| U8 | FartController | 방귀 카운터 + ToonFX 파티클 | U1, U6 |
| U9 | EndingController | SuccessEnd/PoopEnd 연출 | U7, U8 |

## 구현 순서 (순차 - Agent Teams 없음)
1차: U1 + U2 (독립, 빠름)
2차: U3 + U4 (독립 병행 가능)
3차: U5 + U6 (2차 완료 후)
4차: U7 + U8 (3차 완료 후)
5차: U9 (4차 완료 후)

## 파일 목록
```
Assets/Scripts/
├── GameState.cs
├── PenguinGameSettings.cs
├── GameStateMachine.cs
├── SwipeInputController.cs
├── PantyController.cs
├── ButtockController.cs
├── RednessController.cs
├── FartController.cs
└── EndingController.cs
```

## Phase 2.5 Red Team 결과

### RT-API [Medium]
- DOTween + Unity 6: 설치됨, 호환 OK
- AllIn1SpriteShader: 2D 전용 → **3D redness에 사용 불가**. 대안: `material.color` + `DOTween.To` 또는 URP MaterialPropertyBlock

### RT-STATE [Medium]
- ScriptableObject: Resources.Load 대신 [CreateAssetMenu] + Inspector 할당 방식으로
- Awake 순서: GameStateMachine이 마지막 초기화. 다른 컨트롤러는 이벤트/델리게이트로 구독

### RT-DATA [Low]
- redness: float 0~1 범위. Material의 _BaseColor(URP) 또는 _Color(Standard) lerp

### RT-CASCADE [Low]
- 팬티 벗기기: Input.GetTouch delta.y → transform.position.y 직접 매핑 (clamp 필요)
- 팬티 벗기기 완료 조건: position.y가 특정 threshold 이하 → PullingPanty → Playing 전환
