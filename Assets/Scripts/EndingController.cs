using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Dopamine.SlashPenguin
{
    public class EndingController : MonoBehaviour
    {
        [SerializeField] private PenguinGameSettings _settings;
        [SerializeField] private Image _poopOverlay;

        [SerializeField] private ButtockController _buttockController;
        [SerializeField] private PantyController _pantyController;
        [SerializeField] private RednessController _rednessController;
        [SerializeField] private FartController _fartController;

        private GameStateMachine _stateMachine;

        void Start()
        {
            _stateMachine = GameStateMachine.Instance;
            _stateMachine.OnStateChanged += OnStateChanged;

            if (_poopOverlay != null)
            {
                _poopOverlay.color = new Color(1f, 1f, 1f, 0f);
                _poopOverlay.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if (_stateMachine != null) _stateMachine.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.SuccessEnd) OnSuccessEnd();
            else if (state == GameState.PoopEnd) OnPoopEnd();
        }

        private void OnSuccessEnd()
        {
            // 이미션은 RednessController에서 처리됨. 재시작 입력 대기
            RegisterRestartListener();
        }

        private void OnPoopEnd()
        {
            if (_poopOverlay != null)
            {
                _poopOverlay.gameObject.SetActive(true);
                _poopOverlay.color = new Color(1f, 1f, 1f, 0f);
                _poopOverlay.DOFade(1f, _settings.poopCoverDuration)
                    .SetEase(Ease.InCubic)
                    .OnComplete(RegisterRestartListener);
            }
            else
            {
                RegisterRestartListener();
            }
        }

        private void RegisterRestartListener()
        {
            if (SwipeInputController.Instance != null)
                SwipeInputController.Instance.OnTouchBegan += OnRestartTouch;
        }

        private void OnRestartTouch(Vector2 pos)
        {
            if (SwipeInputController.Instance != null)
                SwipeInputController.Instance.OnTouchBegan -= OnRestartTouch;
            ResetGame();
        }

        private void ResetGame()
        {
            if (_poopOverlay != null)
            {
                _poopOverlay.DOFade(0f, 0.3f)
                    .OnComplete(() =>
                    {
                        if (_poopOverlay != null)
                            _poopOverlay.gameObject.SetActive(false);
                    });
            }

            _buttockController?.ResetState();
            _pantyController?.ResetPanty();
            _rednessController?.ResetRedness();
            _fartController?.ResetFart();
            _stateMachine?.TransitionTo(GameState.WaitingStart);
        }
    }
}
