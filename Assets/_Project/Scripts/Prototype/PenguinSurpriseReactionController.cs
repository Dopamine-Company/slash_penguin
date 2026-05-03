using System;
using DG.Tweening;
using UnityEngine;

public class PenguinSurpriseReactionController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform reactionRoot;
    [SerializeField] private Transform[] additionalReactionRoots;
    [SerializeField] private GameObject[] transientSprites;
    [SerializeField] private bool hideSpritesOnAwake = true;

    [Header("Scale Flinch")]
    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private float scaleUpDuration = 0.08f;
    [SerializeField] private float scaleReturnDuration = 0.14f;
    [SerializeField] private Ease scaleUpEase = Ease.OutBack;
    [SerializeField] private Ease scaleReturnEase = Ease.OutQuad;

    [Header("Shake")]
    [SerializeField] private Vector3 punchPosition = new Vector3(0f, 0.04f, 0f);
    [SerializeField] private Vector3 punchRotation = new Vector3(0f, 0f, 4f);
    [SerializeField] private float punchDuration = 0.18f;
    [SerializeField] private int vibrato = 8;
    [SerializeField] private float elasticity = 0.65f;

    [Header("Sprite Timing")]
    [SerializeField] private float spriteVisibleDuration = 0.28f;

    private Sequence currentSequence;

    private void Awake()
    {
        if (reactionRoot == null)
        {
            reactionRoot = transform;
        }

        if (hideSpritesOnAwake)
        {
            SetSpritesActive(false);
        }
    }

    public void Play(Action onComplete = null)
    {
        if (reactionRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        ReactionState[] states = BuildReactionStates();
        if (states.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        currentSequence?.Kill();
        KillTargets(states);
        RestoreTargets(states);

        SetSpritesActive(true);

        currentSequence = DOTween.Sequence();
        foreach (ReactionState state in states)
        {
            Transform target = state.Target;
            currentSequence.Insert(
                0f,
                target
                    .DOScale(state.LocalScale * scaleMultiplier, scaleUpDuration)
                    .SetEase(scaleUpEase));
            currentSequence.Insert(
                scaleUpDuration,
                target
                    .DOScale(state.LocalScale, scaleReturnDuration)
                    .SetEase(scaleReturnEase));
            currentSequence.Insert(
                0f,
                target
                    .DOPunchPosition(punchPosition, punchDuration, vibrato, elasticity));
            currentSequence.Insert(
                0f,
                target
                    .DOPunchRotation(punchRotation, punchDuration, vibrato, elasticity));
        }

        currentSequence.InsertCallback(spriteVisibleDuration, () => SetSpritesActive(false));
        currentSequence.OnComplete(() =>
        {
            SetSpritesActive(false);
            RestoreTargets(states);
            onComplete?.Invoke();
        });
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
        currentSequence = null;

        foreach (Transform target in EnumerateTargets())
        {
            if (target != null)
            {
                target.DOKill();
            }
        }
    }

    private ReactionState[] BuildReactionStates()
    {
        Transform[] targets = EnumerateTargets();
        ReactionState[] states = new ReactionState[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            states[i] = new ReactionState(
                target,
                target.localPosition,
                target.localRotation,
                target.localScale);
        }

        return states;
    }

    private Transform[] EnumerateTargets()
    {
        int additionalCount = additionalReactionRoots != null ? additionalReactionRoots.Length : 0;
        Transform[] targets = new Transform[1 + additionalCount];
        int count = 0;

        if (reactionRoot != null)
        {
            targets[count] = reactionRoot;
            count++;
        }

        if (additionalReactionRoots != null)
        {
            foreach (Transform target in additionalReactionRoots)
            {
                if (target == null || ContainsTarget(targets, count, target))
                {
                    continue;
                }

                targets[count] = target;
                count++;
            }
        }

        if (count == targets.Length)
        {
            return targets;
        }

        Transform[] compactTargets = new Transform[count];
        Array.Copy(targets, compactTargets, count);
        return compactTargets;
    }

    private static bool ContainsTarget(Transform[] targets, int count, Transform target)
    {
        for (int i = 0; i < count; i++)
        {
            if (targets[i] == target)
            {
                return true;
            }
        }

        return false;
    }

    private static void KillTargets(ReactionState[] states)
    {
        foreach (ReactionState state in states)
        {
            if (state.Target != null)
            {
                state.Target.DOKill();
            }
        }
    }

    private static void RestoreTargets(ReactionState[] states)
    {
        foreach (ReactionState state in states)
        {
            if (state.Target == null)
            {
                continue;
            }

            state.Target.localPosition = state.LocalPosition;
            state.Target.localRotation = state.LocalRotation;
            state.Target.localScale = state.LocalScale;
        }
    }

    private void SetSpritesActive(bool active)
    {
        if (transientSprites == null || transientSprites.Length == 0)
        {
            return;
        }

        foreach (GameObject spriteObject in transientSprites)
        {
            if (spriteObject != null)
            {
                spriteObject.SetActive(active);
            }
        }
    }

    private struct ReactionState
    {
        public ReactionState(Transform target, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            Target = target;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
        }

        public readonly Transform Target;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalScale;
    }
}
