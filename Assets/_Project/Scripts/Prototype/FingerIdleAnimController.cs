using DG.Tweening;
using UnityEngine;

public class FingerIdleAnimController : MonoBehaviour
{
    [SerializeField] private float topY = 0.94f;
    [SerializeField] private float bottomY = 0.77f;
    [SerializeField] private float halfDuration = 0.45f;
    [SerializeField] private Ease ease = Ease.InOutSine;

    private Tween _idleTween;
    private bool _dismissed;

    private void Start()
    {
        _idleTween = transform
            .DOLocalMoveY(bottomY, halfDuration)
            .SetEase(ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void Update()
    {
        if (_dismissed) return;
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            Dismiss();
    }

    private void Dismiss()
    {
        _dismissed = true;
        _idleTween?.Kill();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _idleTween?.Kill();
    }
}
