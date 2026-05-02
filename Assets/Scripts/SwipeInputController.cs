using UnityEngine;
using System;

namespace Dopamine.SlashPenguin
{
    public class SwipeInputController : MonoBehaviour
    {
        public static SwipeInputController Instance { get; private set; }

        [SerializeField] private PenguinGameSettings _settings;

        public event Action<Vector2> OnTouchBegan;
        public event Action<Vector2, Vector2> OnTouchDelta;
        public event Action OnTouchEnded;
        public event Action<Vector2> OnSwipeDown;

        private Vector2 _touchStartPos;
        private bool _isTouching;

        // 스와이프 발생 시 시작 위치 (이벤트 핸들러에서 좌/우 판정용)
        public Vector2 LastSwipeStartPos { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _touchStartPos = Input.mousePosition;
                _isTouching = true;
                OnTouchBegan?.Invoke(_touchStartPos);
            }
            else if (Input.GetMouseButton(0) && _isTouching)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _touchStartPos;
                OnTouchDelta?.Invoke(Input.mousePosition, delta);
            }
            else if (Input.GetMouseButtonUp(0) && _isTouching)
            {
                _isTouching = false;
                Vector2 delta = (Vector2)Input.mousePosition - _touchStartPos;
                if (delta.magnitude >= _settings.swipeMinDistance)
                {
                    LastSwipeStartPos = _touchStartPos;
                    OnSwipeDown?.Invoke(delta.normalized);
                }
                OnTouchEnded?.Invoke();
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStartPos = touch.position;
                    _isTouching = true;
                    OnTouchBegan?.Invoke(_touchStartPos);
                    break;
                case TouchPhase.Moved:
                    OnTouchDelta?.Invoke(touch.position, touch.position - _touchStartPos);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    _isTouching = false;
                    Vector2 swipeDelta = touch.position - _touchStartPos;
                    if (swipeDelta.magnitude >= _settings.swipeMinDistance)
                    {
                        LastSwipeStartPos = _touchStartPos;
                        OnSwipeDown?.Invoke(swipeDelta.normalized);
                    }
                    OnTouchEnded?.Invoke();
                    break;
            }
        }
    }
}
