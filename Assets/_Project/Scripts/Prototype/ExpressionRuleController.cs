using System;
using System.Collections.Generic;
using UnityEngine;

public class ExpressionRuleController : MonoBehaviour
{
    [Serializable]
    public class ScorePersistentRule
    {
        [Range(0f, 1f)] public float minScoreRatio;
        [Range(0f, 1f)] public float maxScoreRatio = 1f;
        public ExpressionPreset persistentPreset;
    }

    [Serializable]
    public class MinusScoreRule
    {
        public int minusScore;
        public ExpressionPreset transientPreset;
        public float duration = 0.35f;
    }

    [Header("References")]
    [SerializeField] private GameLoopController gameLoopController;
    [SerializeField] private ExpressionController expressionController;

    [Header("Score Expressions")]
    [SerializeField] private ExpressionPreset fallbackPersistentPreset;
    [SerializeField] private List<ScorePersistentRule> scorePersistentRules = new List<ScorePersistentRule>();
    [SerializeField] private ExpressionPreset earlyCorrectHitPreset;
    [SerializeField] private ExpressionPreset lateCorrectHitPreset;
    [SerializeField] private float correctHitDuration = 0.25f;

    [Header("Miss Expressions")]
    [SerializeField] private List<MinusScoreRule> minusScoreRules = new List<MinusScoreRule>();

    [Header("End Expressions")]
    [SerializeField] private ExpressionPreset successPersistentPreset;
    [SerializeField] private ExpressionPreset failPersistentPreset;

    private void Awake()
    {
        RefreshPersistentExpression();
    }

    private void OnEnable()
    {
        SubscribeToGameLoop();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameLoop();
    }

    public void HandleScoreIncreased(int score)
    {
        if (expressionController == null)
        {
            return;
        }

        RefreshPersistentExpression();

        ExpressionPreset transientPreset = IsPastScoreThreshold(score)
            ? lateCorrectHitPreset
            : earlyCorrectHitPreset;

        if (transientPreset != null)
        {
            expressionController.PlayTransient(transientPreset, correctHitDuration);
        }
    }

    public void HandleMinusScoreIncreased(int minusScore)
    {
        if (expressionController == null)
        {
            return;
        }

        MinusScoreRule rule = FindMinusScoreRule(minusScore);

        if (rule != null && rule.transientPreset != null)
        {
            expressionController.PlayTransient(rule.transientPreset, rule.duration);
        }
    }

    public void HandleResetOrStart()
    {
        RefreshPersistentExpression();
    }

    public void HandleSuccess()
    {
        if (expressionController != null && successPersistentPreset != null)
        {
            expressionController.ApplyPersistent(successPersistentPreset);
        }
    }

    public void HandleFail()
    {
        if (expressionController != null && failPersistentPreset != null)
        {
            expressionController.ApplyPersistent(failPersistentPreset);
        }
    }

    private void SubscribeToGameLoop()
    {
        if (gameLoopController == null)
        {
            return;
        }

        gameLoopController.OnScoreIncreased.AddListener(HandleScoreIncreased);
        gameLoopController.OnMinusScoreIncreased.AddListener(HandleMinusScoreIncreased);
        gameLoopController.OnGameLoopStarted.AddListener(HandleResetOrStart);
        gameLoopController.OnGameLoopReset.AddListener(HandleResetOrStart);
        gameLoopController.OnSuccessGameEnd.AddListener(HandleSuccess);
        gameLoopController.OnFailGameEnd.AddListener(HandleFail);
    }

    private void UnsubscribeFromGameLoop()
    {
        if (gameLoopController == null)
        {
            return;
        }

        gameLoopController.OnScoreIncreased.RemoveListener(HandleScoreIncreased);
        gameLoopController.OnMinusScoreIncreased.RemoveListener(HandleMinusScoreIncreased);
        gameLoopController.OnGameLoopStarted.RemoveListener(HandleResetOrStart);
        gameLoopController.OnGameLoopReset.RemoveListener(HandleResetOrStart);
        gameLoopController.OnSuccessGameEnd.RemoveListener(HandleSuccess);
        gameLoopController.OnFailGameEnd.RemoveListener(HandleFail);
    }

    private void RefreshPersistentExpression()
    {
        if (gameLoopController == null || expressionController == null)
        {
            return;
        }

        float ratio = gameLoopController.TargetScore > 0
            ? Mathf.Clamp01((float)gameLoopController.Score / gameLoopController.TargetScore)
            : 0f;

        ExpressionPreset preset = FindScorePersistentPreset(ratio);

        if (preset != null)
        {
            expressionController.ApplyPersistent(preset);
        }
        else if (fallbackPersistentPreset != null)
        {
            expressionController.ApplyPersistent(fallbackPersistentPreset);
        }
        else
        {
            ExpressionPreset firstPreset = FindFirstScorePersistentPreset();

            if (firstPreset != null)
            {
                expressionController.ApplyPersistent(firstPreset);
            }
        }
    }

    private bool IsPastScoreThreshold(int score)
    {
        if (gameLoopController == null || gameLoopController.TargetScore <= 0)
        {
            return false;
        }

        float ratio = (float)score / gameLoopController.TargetScore;
        return ratio >= gameLoopController.ScoreThresholdRatio;
    }

    private ExpressionPreset FindScorePersistentPreset(float scoreRatio)
    {
        for (int i = 0; i < scorePersistentRules.Count; i++)
        {
            ScorePersistentRule rule = scorePersistentRules[i];

            if (rule == null || rule.persistentPreset == null)
            {
                continue;
            }

            if (scoreRatio >= rule.minScoreRatio && scoreRatio < rule.maxScoreRatio)
            {
                return rule.persistentPreset;
            }
        }

        return null;
    }

    private ExpressionPreset FindFirstScorePersistentPreset()
    {
        for (int i = 0; i < scorePersistentRules.Count; i++)
        {
            ScorePersistentRule rule = scorePersistentRules[i];

            if (rule != null && rule.persistentPreset != null)
            {
                return rule.persistentPreset;
            }
        }

        return null;
    }

    private MinusScoreRule FindMinusScoreRule(int minusScore)
    {
        MinusScoreRule fallbackRule = null;

        for (int i = 0; i < minusScoreRules.Count; i++)
        {
            MinusScoreRule rule = minusScoreRules[i];

            if (rule == null || rule.transientPreset == null)
            {
                continue;
            }

            if (rule.minusScore == minusScore)
            {
                return rule;
            }

            if (rule.minusScore < minusScore)
            {
                fallbackRule = rule;
            }
        }

        return fallbackRule;
    }
}
