using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace Dopamine.SlashPenguin
{
    public enum ButtockSide { Left, Right }

    public class ButtockController : MonoBehaviour
    {
        [SerializeField] private PenguinGameSettings _settings;
        [SerializeField] private Transform _leftButtock;
        [SerializeField] private Transform _rightButtock;
        [SerializeField] private RednessController _rednessController;
        [SerializeField] private FartController _fartController;

        private ButtockSide? _activeSide;
        private bool _isActive;
        private Vector3 _leftOriginalScale;
        private Vector3 _rightOriginalScale;
        private Coroutine _cycleRoutine;
        private GameStateMachine _stateMachine;

        void Start()
        {
            _stateMachine = GameStateMachine.Instance;

            if (_leftButtock != null) _leftOriginalScale = _leftButtock.localScale;
            if (_rightButtock != null) _rightOriginalScale = _rightButtock.localScale;

            _stateMachine.OnStateChanged += OnStateChanged;
            SwipeInputController.Instance.OnSwipeDown += OnSwipe;
        }

        void OnDestroy()
        {
            if (_stateMachine != null) _stateMachine.OnStateChanged -= OnStateChanged;
            if (SwipeInputController.Instance != null) SwipeInputController.Instance.OnSwipeDown -= OnSwipe;
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing) StartCycle();
            else StopCycle();
        }

        private void StartCycle()
        {
            if (_cycleRoutine != null) StopCoroutine(_cycleRoutine);
            _cycleRoutine = StartCoroutine(CycleRoutine());
        }

        private void StopCycle()
        {
            if (_cycleRoutine != null) { StopCoroutine(_cycleRoutine); _cycleRoutine = null; }
            ResetScale();
            _isActive = false;
            _activeSide = null;
        }

        private IEnumerator CycleRoutine()
        {
            while (_stateMachine.CurrentState == GameState.Playing)
            {
                _activeSide = Random.value > 0.5f ? ButtockSide.Left : ButtockSide.Right;
                _isActive = true;
                Expand(_activeSide.Value);

                yield return new WaitForSeconds(_settings.buttockActiveDuration);

                Shrink(_activeSide.Value);
                _isActive = false;
                _activeSide = null;

                yield return new WaitForSeconds(_settings.buttockCooldownDuration);
            }
        }

        private void Expand(ButtockSide side)
        {
            Transform t = side == ButtockSide.Left ? _leftButtock : _rightButtock;
            Vector3 original = side == ButtockSide.Left ? _leftOriginalScale : _rightOriginalScale;
            if (t != null)
                t.DOScale(original * _settings.buttockScaleMultiplier, _settings.buttockScaleDuration).SetEase(Ease.OutBack);
        }

        private void Shrink(ButtockSide side)
        {
            Transform t = side == ButtockSide.Left ? _leftButtock : _rightButtock;
            Vector3 original = side == ButtockSide.Left ? _leftOriginalScale : _rightOriginalScale;
            if (t != null)
                t.DOScale(original, _settings.buttockScaleDuration).SetEase(Ease.InBack);
        }

        private void ResetScale()
        {
            if (_leftButtock != null) _leftButtock.DOScale(_leftOriginalScale, 0.2f);
            if (_rightButtock != null) _rightButtock.DOScale(_rightOriginalScale, 0.2f);
        }

        private void OnSwipe(Vector2 normalizedDir)
        {
            if (_stateMachine.CurrentState != GameState.Playing) return;

            // 스와이프 시작 위치(LastSwipeStartPos)로 좌/우 판정
            ButtockSide hitSide = SwipeInputController.Instance.LastSwipeStartPos.x < Screen.width * 0.5f
                ? ButtockSide.Left
                : ButtockSide.Right;

            if (_isActive && _activeSide.HasValue && _activeSide.Value == hitSide)
                _rednessController?.AddRedness(_settings.rednessIncrement);
            else
                _fartController?.TriggerFart();
        }

        public void ResetState()
        {
            StopCycle();
        }
    }
}
