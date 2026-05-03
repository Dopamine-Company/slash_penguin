using DG.Tweening;
using UnityEngine;

public class ButtHeatController : MonoBehaviour
{
    [Header("Game Loop")]
    [SerializeField] private GameLoopController gameLoopController;

    [Header("Renderers")]
    [SerializeField] private Renderer leftButtRenderer;
    [SerializeField] private Renderer rightButtRenderer;

    [Header("Redness")]
    [SerializeField] private string rednessProperty = "_Redness";
    [SerializeField] private bool useColorFallback = true;
    [SerializeField] private Color rednessColor = new Color(1f, 0.12f, 0.08f, 1f);
    [SerializeField] private float rednessPerHit = 0.12f;
    [SerializeField] private float maxRedness = 1f;
    [SerializeField] private float heatUpDuration = 0.12f;
    [SerializeField] private float coolDownDuration = 0.16f;

    private readonly MaterialPropertyBlock leftBlock = new MaterialPropertyBlock();
    private readonly MaterialPropertyBlock rightBlock = new MaterialPropertyBlock();

    private Color leftBaseColor = Color.white;
    private Color rightBaseColor = Color.white;
    private int leftHitCount;
    private int rightHitCount;
    private float leftVisibleRedness;
    private float rightVisibleRedness;
    private Tween leftTween;
    private Tween rightTween;

    private void Awake()
    {
        if (gameLoopController == null)
        {
            gameLoopController = GetComponent<GameLoopController>();
        }

        leftBaseColor = GetRendererBaseColor(leftButtRenderer);
        rightBaseColor = GetRendererBaseColor(rightButtRenderer);
        ApplyRedness(ButtTarget.Left, 0f);
        ApplyRedness(ButtTarget.Right, 0f);
    }

    private void OnEnable()
    {
        if (gameLoopController == null)
        {
            return;
        }

        gameLoopController.OnCorrectButtHit.AddListener(HandleCorrectButtHit);
        gameLoopController.OnPenalty.AddListener(HandlePenalty);
        gameLoopController.OnGameLoopReset.AddListener(ResetHeat);
    }

    private void OnDisable()
    {
        if (gameLoopController == null)
        {
            return;
        }

        gameLoopController.OnCorrectButtHit.RemoveListener(HandleCorrectButtHit);
        gameLoopController.OnPenalty.RemoveListener(HandlePenalty);
        gameLoopController.OnGameLoopReset.RemoveListener(ResetHeat);
    }

    private void OnDestroy()
    {
        leftTween?.Kill();
        rightTween?.Kill();
    }

    private void HandleCorrectButtHit(ButtTarget hitTarget)
    {
        if (hitTarget == ButtTarget.Left)
        {
            leftHitCount++;
            TweenRedness(ButtTarget.Left, GetRednessForHitCount(leftHitCount));
            return;
        }

        if (hitTarget == ButtTarget.Right)
        {
            rightHitCount++;
            TweenRedness(ButtTarget.Right, GetRednessForHitCount(rightHitCount));
            return;
        }

        if (hitTarget == ButtTarget.Both)
        {
            leftHitCount++;
            rightHitCount++;
            TweenRedness(ButtTarget.Left, GetRednessForHitCount(leftHitCount));
            TweenRedness(ButtTarget.Right, GetRednessForHitCount(rightHitCount));
        }
    }

    private void HandlePenalty(ButtTarget penaltyTarget)
    {
        leftTween?.Kill();
        rightTween?.Kill();

        leftTween = DOTween
            .To(() => leftVisibleRedness, value => ApplyRedness(ButtTarget.Left, value), 0f, coolDownDuration)
            .SetEase(Ease.OutQuad);
        rightTween = DOTween
            .To(() => rightVisibleRedness, value => ApplyRedness(ButtTarget.Right, value), 0f, coolDownDuration)
            .SetEase(Ease.OutQuad);
    }

    private void ResetHeat()
    {
        leftHitCount = 0;
        rightHitCount = 0;
        leftTween?.Kill();
        rightTween?.Kill();
        ApplyRedness(ButtTarget.Left, 0f);
        ApplyRedness(ButtTarget.Right, 0f);
    }

    private float GetRednessForHitCount(int hitCount)
    {
        return Mathf.Clamp(hitCount * rednessPerHit, 0f, maxRedness);
    }

    private void TweenRedness(ButtTarget target, float targetRedness)
    {
        if (target == ButtTarget.Left)
        {
            leftTween?.Kill();
            leftTween = DOTween
                .To(() => leftVisibleRedness, value => ApplyRedness(ButtTarget.Left, value), targetRedness, heatUpDuration)
                .SetEase(Ease.OutQuad);
            return;
        }

        if (target == ButtTarget.Right)
        {
            rightTween?.Kill();
            rightTween = DOTween
                .To(() => rightVisibleRedness, value => ApplyRedness(ButtTarget.Right, value), targetRedness, heatUpDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    private void ApplyRedness(ButtTarget target, float redness)
    {
        redness = Mathf.Clamp(redness, 0f, maxRedness);

        if (target == ButtTarget.Left)
        {
            leftVisibleRedness = redness;
            ApplyRendererRedness(leftButtRenderer, leftBlock, leftBaseColor, redness);
        }
        else if (target == ButtTarget.Right)
        {
            rightVisibleRedness = redness;
            ApplyRendererRedness(rightButtRenderer, rightBlock, rightBaseColor, redness);
        }
    }

    private void ApplyRendererRedness(Renderer targetRenderer, MaterialPropertyBlock block, Color baseColor, float redness)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(block);
        block.SetFloat(rednessProperty, redness);

        if (useColorFallback)
        {
            Color color = Color.Lerp(baseColor, rednessColor, redness);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
        }

        targetRenderer.SetPropertyBlock(block);
    }

    private static Color GetRendererBaseColor(Renderer targetRenderer)
    {
        if (targetRenderer == null || targetRenderer.sharedMaterial == null)
        {
            return Color.white;
        }

        Material material = targetRenderer.sharedMaterial;
        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }
}
