using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class PantyController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static Action OnPantyPulledDown;

    [SerializeField] private float dragThreshold = 200f;
    [SerializeField] private float pullDownDistance = 220f;
    [SerializeField] private float firstStepRatio = 0.25f;
    [SerializeField] private float firstStepDuration = 0.22f;
    [SerializeField] private float secondStepDuration = 0.78f;
    [SerializeField] private float preMoveDelay = 0.05f;
    [SerializeField] private float fadeDelay = 0.12f;
    [SerializeField] private float fadeDuration = 0.7f;
    [SerializeField] private Ease firstStepEase = Ease.OutQuad;
    [SerializeField] private Ease secondStepEase = Ease.InOutSine;
    [Header("Intro Event Timing")]
    [SerializeField] private bool invokeIntroBeforePullComplete = true;
    [SerializeField] private float introStartDelayAfterPull = 0.12f;
    [Header("Bottom Pivot Scale")]
    [SerializeField] private Transform scalePivot;
    [SerializeField] private bool scaleWhileDragging = true;
    [SerializeField] private float dragDistanceForMinScale = 220f;
    [SerializeField] private float upwardDragDistanceForMaxScale = 140f;
    [SerializeField] private float minScaleYMultiplier = 0.35f;
    [SerializeField] private float maxScaleYMultiplier = 1.15f;
    [SerializeField] private float releaseScaleReturnDuration = 0.15f;
    [SerializeField] private Ease releaseScaleReturnEase = Ease.OutQuad;

    private CanvasGroup canvasGroup;
    private float beginDragScreenY;
    private Vector3 beginDragScale;
    private bool isPulledDown;
    private Tween pullTween;
    private Tween scaleTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPulledDown)
        {
            return;
        }

        beginDragScreenY = eventData.position.y;
        beginDragScale = GetScaleTarget().localScale;
        scaleTween?.Kill();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPulledDown)
        {
            return;
        }

        float draggedDownDistance = beginDragScreenY - eventData.position.y;
        UpdateDragScale(draggedDownDistance);

        if (draggedDownDistance < dragThreshold)
        {
            return;
        }

        isPulledDown = true;
        PlayPullDownSequence();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPulledDown)
        {
            return;
        }

        ReturnScaleToBeginDragScale();
    }

    private void PlayPullDownSequence()
    {
        pullTween?.Kill();

        float clampedFirstStepRatio = Mathf.Clamp01(firstStepRatio);
        float firstStepDistance = pullDownDistance * clampedFirstStepRatio;
        float startY = transform.position.y;
        float firstTargetY = startY - firstStepDistance;
        float finalTargetY = startY - pullDownDistance;

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(preMoveDelay);
        sequence.Append(transform.DOMoveY(firstTargetY, firstStepDuration).SetEase(firstStepEase));
        sequence.Append(transform.DOMoveY(finalTargetY, secondStepDuration).SetEase(secondStepEase));

        if (canvasGroup != null)
        {
            float fadeStartTime = preMoveDelay + fadeDelay;
            sequence.Insert(fadeStartTime, canvasGroup.DOFade(0f, fadeDuration));
        }

        float pullCompleteTime = preMoveDelay + firstStepDuration + secondStepDuration;
        if (invokeIntroBeforePullComplete)
        {
            float introEventTime = preMoveDelay + firstStepDuration + Mathf.Max(0f, introStartDelayAfterPull);
            introEventTime = Mathf.Clamp(introEventTime, 0f, Mathf.Max(0f, pullCompleteTime - 0.01f));
            sequence.InsertCallback(introEventTime, () => OnPantyPulledDown?.Invoke());
        }
        else
        {
            if (introStartDelayAfterPull > 0f)
            {
                sequence.AppendInterval(introStartDelayAfterPull);
            }

            sequence.AppendCallback(() => OnPantyPulledDown?.Invoke());
        }
        sequence.OnComplete(() =>
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
            }
        });

        pullTween = sequence;
    }

    private void OnDestroy()
    {
        pullTween?.Kill();
        pullTween = null;
        scaleTween?.Kill();
        scaleTween = null;
    }

    private void UpdateDragScale(float draggedDownDistance)
    {
        if (!scaleWhileDragging)
        {
            return;
        }

        Transform target = GetScaleTarget();
        float scaleYMultiplier;

        if (draggedDownDistance >= 0f)
        {
            float denominator = Mathf.Max(1f, dragDistanceForMinScale);
            float pullRatio = Mathf.Clamp01(draggedDownDistance / denominator);
            scaleYMultiplier = Mathf.Lerp(1f, minScaleYMultiplier, pullRatio);
        }
        else
        {
            float denominator = Mathf.Max(1f, upwardDragDistanceForMaxScale);
            float pushUpRatio = Mathf.Clamp01(-draggedDownDistance / denominator);
            scaleYMultiplier = Mathf.Lerp(1f, maxScaleYMultiplier, pushUpRatio);
        }

        float scaleY = beginDragScale.y * scaleYMultiplier;
        target.localScale = new Vector3(beginDragScale.x, scaleY, beginDragScale.z);
    }

    private void ReturnScaleToBeginDragScale()
    {
        if (!scaleWhileDragging)
        {
            return;
        }

        Transform target = GetScaleTarget();
        scaleTween?.Kill();
        scaleTween = target
            .DOScale(beginDragScale, releaseScaleReturnDuration)
            .SetEase(releaseScaleReturnEase);
    }

    private Transform GetScaleTarget()
    {
        return scalePivot != null ? scalePivot : transform;
    }
}
