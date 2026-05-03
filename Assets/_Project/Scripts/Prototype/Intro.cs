using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Intro : MonoBehaviour
{
    [Header("ZoomUntilEnd")]
    [SerializeField] private float targetCameraZ = -9.5f;
    [Header("UnableWhenIntro")]
    [SerializeField] private GameObject[] unableWhenIntro;
    [Header("UnableWhenZoomInEnd")]
    [SerializeField] private GameObject unableWhenZoomInEnd;
    [Header("IntroButties")]
    [SerializeField] private GameObject[] introButties;
    [Header("HitButties")]
    [SerializeField] private GameObject[] hitButties;
    [Header("ZoomPoint")]
    [SerializeField] private GameObject zoomPoint;
    [Header("MainCamera")]
    [SerializeField] private Camera mainCamera;
    [Header("Timing")]
    [SerializeField] private float zoomInDuration = 1f;
    [SerializeField] private float zoomOutDuration = 1f;
    [Header("IntroButtiesScale")]
    [SerializeField] private float introButtiesZoomInScaleMultiplier = 1.08f;
    [Header("Scene Transition")]
    [SerializeField] private bool loadGameLoopWhenZoomInComplete = true;
    [SerializeField] private string gameLoopSceneName = "GameLoop";
    [SerializeField] private Transform transitionLeftButt;
    [SerializeField] private Transform transitionRightButt;
    [SerializeField] private float holdBeforeConvergeDuration = 0.15f;
    [SerializeField] private float convergeDuration = 0.75f;
    [SerializeField] private float autoStartDelay = 3f;
    [SerializeField] private Ease convergeEase = Ease.InOutSine;

    private Transform cameraTransform;
    private Sequence introSequence;
    private bool _introStarted;
    private bool sceneTransitionStarted;

    private void Start()
    {
        PantyController.OnPantyPulledDown += HandlePantyPulledDown;

        if (unableWhenIntro != null)
        {
            foreach (GameObject go in unableWhenIntro)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
            }
        }
    }

    public void BeginIntro()
    {
        if (_introStarted)
        {
            return;
        }

        _introStarted = true;
        RunIntroCamera();
    }

    private void RunIntroCamera()
    {
        if (zoomPoint == null)
        {
            return;
        }

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        cameraTransform = cam.transform;
        Vector3 startPos = cameraTransform.position;
        Vector3 zoomInPos = GetPositionTowardZoomWithZ(startPos, zoomPoint.transform.position, targetCameraZ);

        List<(Transform transform, Vector3 baseScale)> introButtieScaleEntries = BuildIntroButtieScaleEntries();
        List<(Transform transform, Vector3 baseScale)> hitButtieScaleEntries = BuildHitButtieScaleEntries();

        introSequence?.Kill();
        introSequence = DOTween.Sequence();
        introSequence.Append(cameraTransform.DOMove(zoomInPos, zoomInDuration).SetEase(Ease.InOutQuad));
        JoinIntroButtiesScale(introSequence, introButtieScaleEntries, zoomInDuration, introButtiesZoomInScaleMultiplier);
        introSequence.AppendCallback(() =>
        {
            ApplyAfterZoomInReachedTargetZ();
            SnapHitButtiesToZoomInScale(hitButtieScaleEntries);

            if (loadGameLoopWhenZoomInComplete)
            {
                StartGameLoopSceneTransition();
            }
        });

        if (loadGameLoopWhenZoomInComplete)
        {
            return;
        }

        introSequence.Append(cameraTransform.DOMove(startPos, zoomOutDuration).SetEase(Ease.InOutQuad));
        JoinIntroButtiesScale(introSequence, introButtieScaleEntries, zoomOutDuration, 1f);
        JoinIntroButtiesScale(introSequence, hitButtieScaleEntries, zoomOutDuration, 1f);
        introSequence.OnComplete(DeactivateIntroButties);
    }

    private List<(Transform transform, Vector3 baseScale)> BuildIntroButtieScaleEntries()
    {
        List<(Transform transform, Vector3 baseScale)> entries = new List<(Transform, Vector3)>();
        if (introButties == null || introButties.Length == 0)
        {
            return entries;
        }

        foreach (GameObject go in introButties)
        {
            if (go == null)
            {
                continue;
            }

            Transform tr = go.transform;
            entries.Add((tr, tr.localScale));
        }

        return entries;
    }

    private List<(Transform transform, Vector3 baseScale)> BuildHitButtieScaleEntries()
    {
        List<(Transform transform, Vector3 baseScale)> entries = new List<(Transform, Vector3)>();
        if (hitButties == null || hitButties.Length == 0)
        {
            return entries;
        }

        foreach (GameObject go in hitButties)
        {
            if (go == null)
            {
                continue;
            }

            Transform tr = go.transform;
            entries.Add((tr, tr.localScale));
        }

        return entries;
    }

    private void SnapHitButtiesToZoomInScale(List<(Transform transform, Vector3 baseScale)> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        foreach ((Transform tr, Vector3 baseScale) in entries)
        {
            if (tr == null)
            {
                continue;
            }

            tr.localScale = baseScale * introButtiesZoomInScaleMultiplier;
        }
    }

    private void DeactivateIntroButties()
    {
        if (introButties == null || introButties.Length == 0)
        {
            return;
        }

        foreach (GameObject go in introButties)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }
    }

    private static void JoinIntroButtiesScale(
        Sequence sequence,
        List<(Transform transform, Vector3 baseScale)> entries,
        float duration,
        float multiplier)
    {
        if (sequence == null || entries == null || entries.Count == 0)
        {
            return;
        }

        foreach ((Transform tr, Vector3 baseScale) in entries)
        {
            if (tr == null)
            {
                continue;
            }

            Vector3 targetScale = baseScale * multiplier;
            sequence.Join(tr.DOScale(targetScale, duration).SetEase(Ease.InOutQuad));
        }
    }

    private void ApplyAfterZoomInReachedTargetZ()
    {
        if (unableWhenZoomInEnd != null)
        {
            unableWhenZoomInEnd.SetActive(false);
        }

        if (unableWhenIntro == null)
        {
            return;
        }

        foreach (GameObject go in unableWhenIntro)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }
    }

    private static Vector3 GetPositionTowardZoomWithZ(Vector3 from, Vector3 toward, float targetZ)
    {
        float deltaZ = toward.z - from.z;
        if (Mathf.Approximately(deltaZ, 0f))
        {
            Vector3 adjusted = from;
            adjusted.z = targetZ;
            return adjusted;
        }

        float t = (targetZ - from.z) / deltaZ;
        t = Mathf.Clamp01(t);
        return Vector3.LerpUnclamped(from, toward, t);
    }

    private void OnDestroy()
    {
        PantyController.OnPantyPulledDown -= HandlePantyPulledDown;

        introSequence?.Kill();
        introSequence = null;

        if (cameraTransform != null)
        {
            cameraTransform.DOKill();
        }
    }

    private void HandlePantyPulledDown()
    {
        if (_introStarted)
        {
            return;
        }

        _introStarted = true;
        RunIntroCamera();
    }

    private void StartGameLoopSceneTransition()
    {
        if (sceneTransitionStarted)
        {
            return;
        }

        sceneTransitionStarted = true;

        Transform leftSource = transitionLeftButt != null
            ? transitionLeftButt
            : GetButtieTransformOrNull(0);

        Transform rightSource = transitionRightButt != null
            ? transitionRightButt
            : GetButtieTransformOrNull(1);

        ButtSceneTransitionCarry carry = ButtSceneTransitionCarry.Create(
            leftSource,
            rightSource,
            gameLoopSceneName,
            holdBeforeConvergeDuration,
            convergeDuration,
            autoStartDelay,
            convergeEase);

        carry.StartTransition();
    }

    private Transform GetButtieTransformOrNull(int index)
    {
        if (introButties == null || index < 0 || index >= introButties.Length || introButties[index] == null)
        {
            return null;
        }

        return introButties[index].transform;
    }
}
