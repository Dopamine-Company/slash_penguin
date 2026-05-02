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
    [SerializeField] private float introStartDelayAfterPull = 0.25f;
    [SerializeField] private Ease firstStepEase = Ease.OutQuad;
    [SerializeField] private Ease secondStepEase = Ease.InOutSine;

    private CanvasGroup canvasGroup;
    private float beginDragScreenY;
    private bool isPulledDown;
    private Tween pullTween;

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
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPulledDown)
        {
            return;
        }

        float draggedDownDistance = beginDragScreenY - eventData.position.y;
        if (draggedDownDistance < dragThreshold)
        {
            return;
        }

        isPulledDown = true;
        PlayPullDownSequence();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
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

        float introEventTime = preMoveDelay + firstStepDuration + introStartDelayAfterPull;
        sequence.InsertCallback(introEventTime, () => OnPantyPulledDown?.Invoke());

        if (canvasGroup != null)
        {
            float fadeStartTime = preMoveDelay + fadeDelay;
            sequence.Insert(fadeStartTime, canvasGroup.DOFade(0f, fadeDuration));
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
    }
}
