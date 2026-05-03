using DG.Tweening;
using UnityEngine;

public class StartSceneReturnController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform returnPoint;
    [SerializeField] private float forwardStartDistance = 7f;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.2f;
    [SerializeField] private float returnDuration = 0.45f;
    [SerializeField] private Ease returnEase = Ease.OutCubic;

    private Tween returnTween;

    private void Start()
    {
        if (!SceneReturnState.PlayStartReturnCameraMove)
        {
            return;
        }

        SceneReturnState.PlayStartReturnCameraMove = false;
        PlayReturnMove();
    }

    public void PlayReturnMove()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        Transform cameraTransform = cam.transform;
        Vector3 targetPosition = returnPoint != null ? returnPoint.position : cameraTransform.position;
        Quaternion targetRotation = returnPoint != null ? returnPoint.rotation : cameraTransform.rotation;
        Vector3 startForward = targetRotation * Vector3.forward;

        cameraTransform.position = targetPosition + startForward * forwardStartDistance;
        cameraTransform.rotation = targetRotation;

        returnTween?.Kill();
        returnTween = DOVirtual
            .DelayedCall(startDelay, () =>
            {
                cameraTransform
                    .DOMove(targetPosition, returnDuration)
                    .SetEase(returnEase);
            });
    }

    private void OnDestroy()
    {
        returnTween?.Kill();
        returnTween = null;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam != null)
        {
            cam.transform.DOKill();
        }
    }
}
