using DG.Tweening;
using UnityEngine;

public class JellySwipeSphere : MonoBehaviour
{
    // Requires DOTween. Install/import DOTween if DG.Tweening cannot be resolved.

    private const float MaxPowerSwipeDistance = 600f;

    [Header("Input")]
    [SerializeField] private float minSwipeDistance = 80f;
    [SerializeField] private LayerMask hitLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private float hitMaxDistance = 100f;

    [Header("Movement")]
    [SerializeField] private float pushDistance = 0.35f;
    [SerializeField] private float reboundRatio = 0.45f;
    [SerializeField] private float pushDuration = 0.05f;
    [SerializeField] private float reboundDuration = 0.09f;
    [SerializeField] private float returnDuration = 0.16f;

    [Header("Scale Punch")]
    [SerializeField] private Vector3 punchScale = new Vector3(0.22f, -0.12f, 0.22f);
    [SerializeField] private float punchDuration = 0.22f;
    [SerializeField] private int vibrato = 8;
    [SerializeField] private float elasticity = 0.7f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    private Collider targetCollider;
    private Vector2 pointerDownPosition;
    private bool isPointerTracking;
    private bool didPointerTouchThisSphere;

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

        targetCollider = GetComponent<Collider>();
        CacheBaseTransform();
    }

    private void OnDisable()
    {
        currentSequence?.Kill();
        transform.DOKill();
    }

    private void Update()
    {
        if (HandleTouchInput())
        {
            return;
        }

        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BeginPointer(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            UpdatePointerPath(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndPointer(Input.mousePosition);
        }
    }

    private bool HandleTouchInput()
    {
        if (Input.touchCount <= 0)
        {
            return false;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            BeginPointer(touch.position);
        }
        else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
            UpdatePointerPath(touch.position);
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            EndPointer(touch.position);
        }

        return true;
    }

    private void BeginPointer(Vector2 screenPosition)
    {
        pointerDownPosition = screenPosition;
        isPointerTracking = true;
        didPointerTouchThisSphere = IsPointerOverThisSphere(screenPosition);
    }

    private void UpdatePointerPath(Vector2 screenPosition)
    {
        if (!isPointerTracking || didPointerTouchThisSphere)
        {
            return;
        }

        didPointerTouchThisSphere = IsPointerOverThisSphere(screenPosition);
    }

    private void EndPointer(Vector2 screenPosition)
    {
        if (!isPointerTracking)
        {
            return;
        }

        isPointerTracking = false;

        Vector2 swipeDelta = screenPosition - pointerDownPosition;

        if (!didPointerTouchThisSphere || swipeDelta.magnitude < minSwipeDistance)
        {
            didPointerTouchThisSphere = false;
            return;
        }

        didPointerTouchThisSphere = false;
        PlayJellyReaction(swipeDelta);
    }

    private void PlayJellyReaction(Vector2 swipeDelta)
    {
        Vector2 screenDirection = swipeDelta.normalized;
        Vector3 localDirection = ConvertScreenDirectionToLocalDirection(screenDirection);

        float power = Mathf.Clamp01(swipeDelta.magnitude / MaxPowerSwipeDistance);
        float weightedPower = Mathf.Lerp(0.65f, 1.25f, power);

        Vector3 pushOffset = localDirection * pushDistance * weightedPower;
        Vector3 reboundOffset = -pushOffset * reboundRatio;

        currentSequence?.Kill();
        transform.DOKill();
        ResetToBaseTransform();

        currentSequence = DOTween.Sequence();
        currentSequence.Append(transform
            .DOLocalMove(baseLocalPosition + pushOffset, pushDuration)
            .SetEase(Ease.OutQuad));
        currentSequence.Append(transform
            .DOLocalMove(baseLocalPosition + reboundOffset, reboundDuration)
            .SetEase(Ease.OutBack));
        currentSequence.Append(transform
            .DOLocalMove(baseLocalPosition, returnDuration)
            .SetEase(Ease.OutElastic));

        transform.DOPunchScale(
            punchScale * Mathf.Lerp(0.75f, 1.35f, power),
            punchDuration,
            vibrato,
            elasticity);
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

    private bool IsPointerOverThisSphere(Vector2 screenPosition)
    {
        if (targetCamera == null)
        {
            return false;
        }

        if (targetCollider == null)
        {
            return false;
        }

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);

        if (((1 << targetCollider.gameObject.layer) & hitLayers) == 0)
        {
            return false;
        }

        return targetCollider.Raycast(ray, out _, hitMaxDistance);
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
