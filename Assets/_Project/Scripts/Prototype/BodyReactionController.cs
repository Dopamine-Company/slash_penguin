using DG.Tweening;
using UnityEngine;

public class BodyReactionController : MonoBehaviour
{
    // Requires DOTween. Install/import DOTween if DG.Tweening cannot be resolved.

    [Header("Movement")]
    [SerializeField] private float flinchDistance = 0.08f;
    [SerializeField] private float flinchDuration = 0.045f;
    [SerializeField] private float returnDuration = 0.12f;

    [Header("Rotation")]
    [SerializeField] private float rotationDegrees = 5f;

    [Header("Scale")]
    [SerializeField] private Vector3 flinchScale = new Vector3(0.04f, -0.03f, 0.04f);

    [Header("Power")]
    [SerializeField] private float minPowerMultiplier = 0.65f;
    [SerializeField] private float maxPowerMultiplier = 1.1f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;

    private Sequence currentSequence;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        CacheBaseTransform();
    }

    private void OnDisable()
    {
        currentSequence?.Kill();
        transform.DOKill();
    }

    public void PlayFlinch(Vector2 swipeDelta, float power)
    {
        if (swipeDelta.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector2 screenDirection = swipeDelta.normalized;
        float weightedPower = Mathf.Lerp(minPowerMultiplier, maxPowerMultiplier, Mathf.Clamp01(power));

        Vector3 localDirection = ConvertScreenDirectionToLocalDirection(-screenDirection);
        Vector3 flinchOffset = localDirection * flinchDistance * weightedPower;
        Vector3 scaleOffset = flinchScale * weightedPower;
        Vector3 rotationOffset = new Vector3(screenDirection.y, -screenDirection.x, 0f) * rotationDegrees * weightedPower;

        currentSequence?.Kill();
        transform.DOKill();
        ResetToBaseTransform();

        currentSequence = DOTween.Sequence();
        currentSequence.Append(transform
            .DOLocalMove(baseLocalPosition + flinchOffset, flinchDuration)
            .SetEase(Ease.OutQuad));
        currentSequence.Join(transform
            .DOLocalRotateQuaternion(baseLocalRotation * Quaternion.Euler(rotationOffset), flinchDuration)
            .SetEase(Ease.OutQuad));
        currentSequence.Join(transform
            .DOScale(baseLocalScale + scaleOffset, flinchDuration)
            .SetEase(Ease.OutQuad));

        currentSequence.Append(transform
            .DOLocalMove(baseLocalPosition, returnDuration)
            .SetEase(Ease.OutBack));
        currentSequence.Join(transform
            .DOLocalRotateQuaternion(baseLocalRotation, returnDuration)
            .SetEase(Ease.OutBack));
        currentSequence.Join(transform
            .DOScale(baseLocalScale, returnDuration)
            .SetEase(Ease.OutBack));
    }

    private Vector3 ConvertScreenDirectionToLocalDirection(Vector2 screenDirection)
    {
        if (targetCamera == null)
        {
            return new Vector3(screenDirection.x, screenDirection.y, 0f).normalized;
        }

        Vector3 worldDirection =
            targetCamera.transform.right * screenDirection.x +
            targetCamera.transform.up * screenDirection.y;

        Vector3 localDirection = transform.parent != null
            ? transform.parent.InverseTransformDirection(worldDirection)
            : worldDirection;

        return localDirection.normalized;
    }

    private void CacheBaseTransform()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;
        baseLocalRotation = transform.localRotation;
    }

    private void ResetToBaseTransform()
    {
        transform.localPosition = baseLocalPosition;
        transform.localScale = baseLocalScale;
        transform.localRotation = baseLocalRotation;
    }
}
