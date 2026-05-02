# Phase 1 기획 분석 — slash_penguin 게임잼

## 분석 페이지
- 메인: 게임잼 (3539f69f2f8980ec8673e47c357941cc)
- 하위: slash_penguin 기획 · AI 활용 계획 (3549f69f2f8980ab98f0df60c70cef8d)

---

## 핵심 요구사항 (스펙 신뢰도 포함)

| # | 요구사항 | 신뢰도 | 비고 |
|---|---------|--------|------|
| 1 | 시작 대기: 펭귄 3D 메시가 뒤돌아 있는 장면. 온보딩 없음 | A | 명확 |
| 2 | 시작 트리거: 팬티 터치 후 아래 스와이프 → 스와이프 Y값에 맞춰 팬티 벗겨짐 | B | Y 매핑 방식 구체적 미명시 |
| 3 | 게임 루프: 좌/우 볼기짝이 랜덤으로 커졌다 작아짐 (터치 신호). 가만히 있는 쪽 타격 금지 | A | 명확 |
| 4 | 규칙: 활성 볼기짝 스와이프 → redness 증가 | A | 증가량 B |
| 5 | 규칙: 비활성 볼기짝 스와이프 → 방귀, redness 감소 | A | 감소량 B |
| 6 | 성공 엔딩: redness 최대 → 이미션/텍스처로 조명처럼 빛남 | A | 명확 |
| 7 | 실패 엔딩: 방귀 4회 누적 → 하얀 똥이 화면 전체를 페인트처럼 덮으며 종료 | A | 명확 |
| 8 | 재시작: 엔딩 후 화면 터치 → 1로 복귀 | A | 명확 |

## 상태머신 (AI 활용 계획에서 제안)
- `WaitingStart` → `PullingPanty` → `Playing` → `SuccessEnd` or `PoopEnd`
- 재시작: PoopEnd/SuccessEnd → WaitingStart

## ScriptableObject 튜닝 대상 (B등급)
- 스와이프 거리 임계값
- redness 증감량
- 방귀 임계값 (4회 고정이지만 SO로 분리)
- DOTween/Feel 수치 (볼기짝 팽창 타이밍, 크기)

## 에셋 현황

| 에셋 | 상태 | 경로 |
|------|------|------|
| DOTweenPro | ✅ 설치됨 | Assets/Plugins/Demigiant/DOTweenPro |
| Feel | ✅ 설치됨 | Assets/Feel |
| Juicer | ✅ 설치됨 | Assets/Juicer |
| ToonFX | ✅ 설치됨 | Assets/ToonFX |
| AllIn1SpriteShader | ✅ 설치됨 | Assets/Plugins/AllIn1SpriteShader |
| TransitionBlocks | ✅ 설치됨 | Assets/TransitionBlocks |
| 펭귄 에셋 | ⚠️ 2D 스프라이트 | Assets/Nine Pines Animation/2D Character Sprite Animation - Penguin |
| 펭귄 3D Mesh | ❌ 없음 | 기획: "뽑아야 하는 에셋" 목록 |

## 기존 코드 현황
- 게임 로직 스크립트: **없음** (Assets/Scripts/ 없음)
- 게임 씬: **없음** (Assets/Scenes/ 없음)
- 전부 서드파티 에셋 데모만 존재

## 충돌/모순 분석

### 기획서-구현 모순
| ID | 유형 | 기획 요구사항 | 현재 구현 | 충돌 내용 | 심각도 |
|----|------|-------------|----------|----------|--------|
| I-01 | 누락 인프라 | 펭귄 3D 메시 기반 | 2D 스프라이트만 존재 (Nine Pines) | 3D 메시 에셋 없음 | High |
| I-02 | 누락 인프라 | 게임 씬 + 스크립트 | 전무 | 처음부터 구현 필요 | 참고 (당연) |
| I-03 | 스펙 미완 | "흰박스 프로토타입" 전략 | 3D 에셋 없음 | 흰박스(캡슐+구)로 시작 가능 | Low |

## 메시 구조 권장사항 (기획 AI 계획에서)
- 몸통 + 좌볼기짝 + 우볼기짝 분리
- DOTween + Feel + 필요 시 BlendShape
