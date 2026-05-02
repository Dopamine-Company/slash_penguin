using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public enum ButtTarget
{
    None,
    Left,
    Right,
    Both
}

public class GameLoopController : MonoBehaviour
{
    [Serializable]
    public class BeatSpeedStep
    {
        public float startTime;
        public float beatInterval = 1f;
        public float activeDurationMultiplier = 1f;
    }

    [Serializable]
    public class IntEvent : UnityEvent<int>
    {
    }

    [Serializable]
    public class ButtTargetEvent : UnityEvent<ButtTarget>
    {
    }

    [Serializable]
    public class StringEvent : UnityEvent<string>
    {
    }

    [Header("Targets")]
    [SerializeField] private Transform leftButt;
    [SerializeField] private Transform rightButt;
    [SerializeField] private float inactiveScale = 1f;
    [SerializeField] private float activeScale = 1.2f;

    [Header("Timing")]
    [SerializeField] private float targetActiveDuration = 0.35f;
    [SerializeField] private float minTargetActiveDuration = 0.16f;
    [SerializeField] private List<BeatSpeedStep> beatSpeedSteps = new List<BeatSpeedStep>
    {
        new BeatSpeedStep { startTime = 0f, beatInterval = 1f, activeDurationMultiplier = 1f },
        new BeatSpeedStep { startTime = 10f, beatInterval = 0.8f, activeDurationMultiplier = 0.83f },
        new BeatSpeedStep { startTime = 20f, beatInterval = 0.6f, activeDurationMultiplier = 0.69f },
        new BeatSpeedStep { startTime = 30f, beatInterval = 0.45f, activeDurationMultiplier = 0.58f }
    };

    [Header("Score")]
    [SerializeField] private int targetScore = 30;
    [SerializeField] private int maxMinusScore = 3;
    [SerializeField] private float scoreThresholdRatio = 0.7f;
    [SerializeField] private bool bothRequiresBothHits;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text minusScoreText;
    [SerializeField] private TMP_Text judgementText;
    [SerializeField] private string scorePrefix = "Score: ";
    [SerializeField] private string minusScorePrefix = "Miss: ";

    [Header("Events")]
    public IntEvent OnScoreIncreased;
    public IntEvent OnMinusScoreIncreased;
    public UnityEvent OnScoreThresholdReached;
    public UnityEvent OnSuccessGameEnd;
    public UnityEvent OnFailGameEnd;
    public ButtTargetEvent OnBeatStarted;
    public UnityEvent OnTargetHitWindowClosed;
    public UnityEvent OnBeatEnded;
    public StringEvent OnJudgementMessage;

    public int Score => score;
    public int MinusScore => minusScore;
    public ButtTarget CurrentTarget => currentTarget;
    public bool IsRunning => state == LoopState.Running;
    public string LastJudgementMessage => lastJudgementMessage;

    private enum LoopState
    {
        Idle,
        Running,
        Success,
        Fail
    }

    private Coroutine loopCoroutine;
    private LoopState state = LoopState.Idle;
    private ButtTarget currentTarget = ButtTarget.None;

    private int score;
    private int minusScore;
    private float loopStartTime;
    private bool scoreThresholdTriggered;
    private bool isHitWindowOpen;
    private bool isBeatResolved;
    private bool leftHitThisBeat;
    private bool rightHitThisBeat;
    private Vector3 leftButtBaseScale = Vector3.one;
    private Vector3 rightButtBaseScale = Vector3.one;
    private string lastJudgementMessage;

    private void Awake()
    {
        CacheBaseScales();
        SetButtScales(inactiveScale, inactiveScale);
        RefreshScoreText();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            StartGameLoop();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            RegisterButtHit(ButtTarget.Left);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            RegisterButtHit(ButtTarget.Right);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetLoop();
        }
    }

    public void StartGameLoop()
    {
        if (state == LoopState.Running)
        {
            return;
        }

        ResetLoop();
        state = LoopState.Running;
        loopStartTime = Time.time;
        loopCoroutine = StartCoroutine(GameLoopRoutine());
    }

    public void StopGameLoop()
    {
        StopCurrentLoopCoroutine();
        state = LoopState.Idle;
        CloseHitWindow();
        currentTarget = ButtTarget.None;
    }

    public void ResetLoop()
    {
        StopCurrentLoopCoroutine();

        state = LoopState.Idle;
        currentTarget = ButtTarget.None;
        score = 0;
        minusScore = 0;
        scoreThresholdTriggered = false;
        isHitWindowOpen = false;
        isBeatResolved = false;
        leftHitThisBeat = false;
        rightHitThisBeat = false;

        SetButtScales(inactiveScale, inactiveScale);
        RefreshScoreText();
    }

    public void RegisterButtHit(ButtTarget hitTarget)
    {
        if (state != LoopState.Running || !isHitWindowOpen || isBeatResolved)
        {
            SetJudgementMessage($"Ignored {hitTarget}: no active hit window.");
            return;
        }

        if (hitTarget == ButtTarget.None || hitTarget == ButtTarget.Both)
        {
            RegisterMinusScore($"Miss: invalid hit target {hitTarget}.");
            ResolveBeat();
            return;
        }

        if (!IsHitAllowedForCurrentTarget(hitTarget))
        {
            RegisterMinusScore($"Miss: hit {hitTarget}, target was {currentTarget}.");
            ResolveBeat();
            return;
        }

        if (currentTarget == ButtTarget.Both && bothRequiresBothHits)
        {
            leftHitThisBeat |= hitTarget == ButtTarget.Left;
            rightHitThisBeat |= hitTarget == ButtTarget.Right;

            if (leftHitThisBeat && rightHitThisBeat)
            {
                RegisterScore($"Good: both targets hit.");
                ResolveBeat();
            }
            else
            {
                SetJudgementMessage($"Partial: {hitTarget} hit, waiting for the other butt.");
            }

            return;
        }

        RegisterScore($"Good: hit {hitTarget}, target was {currentTarget}.");
        ResolveBeat();
    }

    private IEnumerator GameLoopRoutine()
    {
        while (state == LoopState.Running)
        {
            float beatInterval = GetCurrentBeatInterval();
            float activeDuration = GetCurrentTargetActiveDuration();

            BeginBeat();

            float activeElapsed = 0f;
            while (state == LoopState.Running && activeElapsed < activeDuration)
            {
                activeElapsed += Time.deltaTime;
                yield return null;
            }

            if (state != LoopState.Running)
            {
                yield break;
            }

            CloseHitWindow();

            if (!isBeatResolved)
            {
                RegisterMinusScore($"Miss: target {currentTarget} timed out.");
                ResolveBeat();
            }

            OnTargetHitWindowClosed?.Invoke();

            if (state != LoopState.Running)
            {
                yield break;
            }

            float waitAfterWindow = Mathf.Max(0f, beatInterval - activeDuration);
            float waitElapsed = 0f;

            while (state == LoopState.Running && waitElapsed < waitAfterWindow)
            {
                waitElapsed += Time.deltaTime;
                yield return null;
            }

            if (state != LoopState.Running)
            {
                yield break;
            }

            OnBeatEnded?.Invoke();
        }
    }

    private void BeginBeat()
    {
        currentTarget = GetRandomTarget();
        isHitWindowOpen = true;
        isBeatResolved = false;
        leftHitThisBeat = false;
        rightHitThisBeat = false;

        ApplyTargetScale(currentTarget);
        OnBeatStarted?.Invoke(currentTarget);
    }

    private void CloseHitWindow()
    {
        isHitWindowOpen = false;
        SetButtScales(inactiveScale, inactiveScale);
    }

    private void ResolveBeat()
    {
        isBeatResolved = true;
        CheckEndConditions();
    }

    private bool IsHitAllowedForCurrentTarget(ButtTarget hitTarget)
    {
        return currentTarget == hitTarget || currentTarget == ButtTarget.Both;
    }

    private void RegisterScore(string reason)
    {
        score++;
        RefreshScoreText();
        SetJudgementMessage(reason);
        OnScoreIncreased?.Invoke(score);
        CheckScoreThreshold();
    }

    private void RegisterMinusScore(string reason)
    {
        minusScore++;
        RefreshScoreText();
        SetJudgementMessage(reason);
        OnMinusScoreIncreased?.Invoke(minusScore);
    }

    private void CheckScoreThreshold()
    {
        if (scoreThresholdTriggered)
        {
            return;
        }

        float threshold = targetScore * scoreThresholdRatio;
        if (score >= threshold)
        {
            scoreThresholdTriggered = true;
            OnScoreThresholdReached?.Invoke();
        }
    }

    private void CheckEndConditions()
    {
        if (score >= targetScore)
        {
            EndGame(LoopState.Success);
            return;
        }

        if (minusScore >= maxMinusScore)
        {
            EndGame(LoopState.Fail);
        }
    }

    private void EndGame(LoopState endState)
    {
        StopCurrentLoopCoroutine();
        state = endState;
        CloseHitWindow();
        currentTarget = ButtTarget.None;

        if (endState == LoopState.Success)
        {
            OnSuccessGameEnd?.Invoke();
        }
        else if (endState == LoopState.Fail)
        {
            OnFailGameEnd?.Invoke();
        }
    }

    private ButtTarget GetRandomTarget()
    {
        int randomValue = UnityEngine.Random.Range(0, 3);

        if (randomValue == 0)
        {
            return ButtTarget.Left;
        }

        if (randomValue == 1)
        {
            return ButtTarget.Right;
        }

        return ButtTarget.Both;
    }

    private float GetCurrentBeatInterval()
    {
        float interval = 1f;
        BeatSpeedStep currentStep = GetCurrentBeatSpeedStep();

        if (currentStep != null)
        {
            interval = currentStep.beatInterval;
        }

        return Mathf.Max(0.01f, interval);
    }

    private float GetCurrentTargetActiveDuration()
    {
        float multiplier = 1f;
        BeatSpeedStep currentStep = GetCurrentBeatSpeedStep();

        if (currentStep != null)
        {
            multiplier = Mathf.Max(0.01f, currentStep.activeDurationMultiplier);
        }

        return Mathf.Max(minTargetActiveDuration, targetActiveDuration * multiplier);
    }

    private BeatSpeedStep GetCurrentBeatSpeedStep()
    {
        float elapsed = Time.time - loopStartTime;
        BeatSpeedStep currentStep = null;

        for (int i = 0; i < beatSpeedSteps.Count; i++)
        {
            BeatSpeedStep step = beatSpeedSteps[i];

            if (step == null || elapsed < step.startTime)
            {
                continue;
            }

            currentStep = step;
        }

        return currentStep;
    }

    private void ApplyTargetScale(ButtTarget target)
    {
        float leftScale = target == ButtTarget.Left || target == ButtTarget.Both
            ? activeScale
            : inactiveScale;

        float rightScale = target == ButtTarget.Right || target == ButtTarget.Both
            ? activeScale
            : inactiveScale;

        SetButtScales(leftScale, rightScale);
    }

    private void SetButtScales(float leftScale, float rightScale)
    {
        if (leftButt != null)
        {
            leftButt.localScale = Vector3.Scale(leftButtBaseScale, Vector3.one * leftScale);
        }

        if (rightButt != null)
        {
            rightButt.localScale = Vector3.Scale(rightButtBaseScale, Vector3.one * rightScale);
        }
    }

    private void CacheBaseScales()
    {
        if (leftButt != null)
        {
            leftButtBaseScale = leftButt.localScale;
        }

        if (rightButt != null)
        {
            rightButtBaseScale = rightButt.localScale;
        }
    }

    private void RefreshScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{scorePrefix}{score}/{targetScore}";
        }

        if (minusScoreText != null)
        {
            minusScoreText.text = $"{minusScorePrefix}{minusScore}/{maxMinusScore}";
        }
    }

    private void SetJudgementMessage(string message)
    {
        lastJudgementMessage = message;

        if (judgementText != null)
        {
            judgementText.text = message;
        }

        OnJudgementMessage?.Invoke(message);
        Debug.Log(message, this);
    }

    private void StopCurrentLoopCoroutine()
    {
        if (loopCoroutine == null)
        {
            return;
        }

        StopCoroutine(loopCoroutine);
        loopCoroutine = null;
    }
}
