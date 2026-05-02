using UnityEngine;
using DG.Tweening;

namespace Dopamine.SlashPenguin
{
    public class PantyController : MonoBehaviour
    {
        [SerializeField] private PenguinGameSettings _settings;
        [SerializeField] private Transform _pantyTransform;

        private Vector3 _initialLocalPosition;
        private bool _isPulling;
        private GameStateMachine _stateMachine;

        void Awake()
        {
            if (_pantyTransform != null)
                _initialLocalPosition = _pantyTransform.localPosition;
        }

        void Start()
        {
            _stateMachine = GameStateMachine.Instance;
            SwipeInputController.Instance.OnTouchDelta += OnTouchDelta;
            SwipeInputController.Instance.OnTouchEnded += OnTouchEnded;
        }

        void OnDestroy()
        {
            if (SwipeInputController.Instance != null)
            {
                SwipeInputController.Instance.OnTouchDelta -= OnTouchDelta;
                SwipeInputController.Instance.OnTouchEnded -= OnTouchEnded;
            }
        }

        private void OnTouchDelta(Vector2 screenPos, Vector2 delta)
        {
            if (_stateMachine.CurrentState != GameState.WaitingStart) return;
            if (!_isPulling && delta.y < -20f) _isPulling = true;
            if (!_isPulling || _pantyTransform == null) return;

            float newY = _initialLocalPosition.y + delta.y * _settings.pantySwipeSensitivity;
            newY = Mathf.Min(newY, _initialLocalPosition.y);

            Vector3 pos = _pantyTransform.localPosition;
            pos.y = newY;
            _pantyTransform.localPosition = pos;

            if (newY <= _settings.pantyPullThreshold)
                CompletePull();
        }

        private void OnTouchEnded()
        {
            if (_stateMachine.CurrentState != GameState.WaitingStart || !_isPulling) return;
            if (_pantyTransform != null && _pantyTransform.localPosition.y > _settings.pantyPullThreshold)
            {
                _isPulling = false;
                _pantyTransform.DOLocalMoveY(_initialLocalPosition.y, 0.3f).SetEase(Ease.OutBounce);
            }
        }

        private void CompletePull()
        {
            _isPulling = false;
            _stateMachine.TransitionTo(GameState.PullingPanty);

            if (_pantyTransform != null)
            {
                _pantyTransform.DOLocalMoveY(_initialLocalPosition.y - 3f, 0.4f)
                    .SetEase(Ease.InCubic)
                    .OnComplete(() =>
                    {
                        if (_pantyTransform != null)
                            _pantyTransform.gameObject.SetActive(false);
                    });
            }

            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (_stateMachine != null)
                    _stateMachine.TransitionTo(GameState.Playing);
            });
        }

        public void ResetPanty()
        {
            if (_pantyTransform == null) return;
            _pantyTransform.gameObject.SetActive(true);
            _pantyTransform.localPosition = _initialLocalPosition;
            _isPulling = false;
        }
    }
}
