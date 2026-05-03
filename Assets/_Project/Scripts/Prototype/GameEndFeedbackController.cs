using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameEndFeedbackController : MonoBehaviour
{
    [Header("Game Loop")]
    [SerializeField] private GameLoopController gameLoopController;
    [SerializeField] private string startSceneName = "Start";

    [Header("Success Shake")]
    [SerializeField] private Transform[] successShakeTargets;
    [SerializeField] private Vector3 successPunchPosition = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private Vector3 successPunchRotation = new Vector3(0f, 0f, 7f);
    [SerializeField] private float successShakeDuration = 1.2f;
    [SerializeField] private int successVibrato = 18;
    [SerializeField] private float successElasticity = 0.75f;

    [Header("Success Rainbow")]
    [SerializeField] private Renderer[] rainbowRenderers;
    [SerializeField] private float rainbowDuration = 1.25f;
    [SerializeField] private float rainbowPulseInterval = 0.09f;
    [SerializeField] private float rainbowBrightness = 2.5f;
    [SerializeField] private float finalWhiteGlowDuration = 0.55f;
    [SerializeField] private float finalWhiteBrightness = 5f;

    [Header("Success White Screen")]
    [SerializeField] private Image successWhiteOverlay;
    [SerializeField] private float successWhiteFadeDelay = 0.75f;
    [SerializeField] private float successWhiteFadeDuration = 0.75f;
    [SerializeField] private float successWhiteHoldDuration = 0.15f;

    [Header("Fail Poop Screen")]
    [SerializeField] private Image failPoopOverlay;
    [SerializeField] private Sprite failPoopSprite;
    [SerializeField] private float failDelay = 0.7f;
    [SerializeField] private float failPopDuration = 0.16f;
    [SerializeField] private float failHoldDuration = 0.25f;
    [SerializeField] private Vector3 failPopStartScale = new Vector3(0.78f, 0.78f, 1f);
    [SerializeField] private Vector3 failPopOvershootScale = new Vector3(1.08f, 1.08f, 1f);

    private readonly Color[] rainbowColors =
    {
        Color.red,
        new Color(1f, 0.55f, 0f),
        Color.yellow,
        Color.green,
        Color.cyan,
        Color.blue,
        new Color(0.75f, 0f, 1f),
        Color.magenta
    };

    private MaterialPropertyBlock propertyBlock;
    private Coroutine endRoutine;
    private bool endingStarted;

    private void Awake()
    {
        if (gameLoopController == null)
        {
            gameLoopController = GetComponent<GameLoopController>();
        }

        propertyBlock = new MaterialPropertyBlock();
        EnsureOverlayImages();
        HideOverlay(successWhiteOverlay);
        HideOverlay(failPoopOverlay);
    }

    private void OnEnable()
    {
        if (gameLoopController == null)
        {
            return;
        }

        gameLoopController.OnSuccessGameEnd.AddListener(PlaySuccessEnd);
        gameLoopController.OnFailGameEnd.AddListener(PlayFailEnd);
    }

    private void OnDisable()
    {
        if (gameLoopController == null)
        {
            return;
        }

        gameLoopController.OnSuccessGameEnd.RemoveListener(PlaySuccessEnd);
        gameLoopController.OnFailGameEnd.RemoveListener(PlayFailEnd);
    }

    public void PlaySuccessEnd()
    {
        if (endingStarted)
        {
            return;
        }

        endingStarted = true;
        StartEndRoutine(SuccessRoutine());
    }

    public void PlayFailEnd()
    {
        if (endingStarted)
        {
            return;
        }

        endingStarted = true;
        StartEndRoutine(FailRoutine());
    }

    private IEnumerator SuccessRoutine()
    {
        EnsureOverlayImages();
        HideOverlay(failPoopOverlay);
        HideOverlay(successWhiteOverlay);

        Sequence sequence = DOTween.Sequence();
        AddShakeTweens(sequence);
        AddRainbowTweens(sequence);
        AddSuccessWhiteOverlayTween(sequence);

        yield return sequence.WaitForCompletion();
        yield return new WaitForSeconds(successWhiteHoldDuration);

        LoadStartSceneWithReturnMove();
    }

    private IEnumerator FailRoutine()
    {
        EnsureOverlayImages();
        HideOverlay(successWhiteOverlay);
        HideOverlay(failPoopOverlay);

        yield return new WaitForSeconds(failDelay);

        Image overlay = failPoopOverlay != null ? failPoopOverlay : successWhiteOverlay;
        if (overlay == null)
        {
            LoadStartSceneWithReturnMove();
            yield break;
        }

        if (failPoopSprite != null)
        {
            overlay.sprite = failPoopSprite;
            overlay.type = Image.Type.Simple;
            overlay.preserveAspect = false;
        }

        ShowOverlay(overlay, 1f);
        overlay.rectTransform.localScale = failPopStartScale;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(overlay.rectTransform.DOScale(failPopOvershootScale, failPopDuration).SetEase(Ease.OutBack));
        sequence.Append(overlay.rectTransform.DOScale(Vector3.one, failPopDuration * 0.6f).SetEase(Ease.OutQuad));

        yield return sequence.WaitForCompletion();
        yield return new WaitForSeconds(failHoldDuration);

        LoadStartSceneWithReturnMove();
    }

    private void StartEndRoutine(IEnumerator routine)
    {
        if (endRoutine != null)
        {
            StopCoroutine(endRoutine);
        }

        endRoutine = StartCoroutine(routine);
    }

    private void AddShakeTweens(Sequence sequence)
    {
        if (sequence == null || successShakeTargets == null)
        {
            return;
        }

        foreach (Transform target in successShakeTargets)
        {
            if (target == null)
            {
                continue;
            }

            target.DOKill();
            sequence.Insert(
                0f,
                target.DOPunchPosition(successPunchPosition, successShakeDuration, successVibrato, successElasticity));
            sequence.Insert(
                0f,
                target.DOPunchRotation(successPunchRotation, successShakeDuration, successVibrato, successElasticity));
        }
    }

    private void AddRainbowTweens(Sequence sequence)
    {
        if (sequence == null || rainbowRenderers == null || rainbowRenderers.Length == 0)
        {
            return;
        }

        float elapsed = 0f;
        int colorIndex = 0;

        while (elapsed < rainbowDuration)
        {
            Color color = rainbowColors[colorIndex % rainbowColors.Length] * rainbowBrightness;
            sequence.InsertCallback(elapsed, () => ApplyRendererColor(color));

            elapsed += Mathf.Max(0.01f, rainbowPulseInterval);
            colorIndex++;
        }

        sequence.Insert(rainbowDuration, DOTween.To(
            () => 0f,
            value => ApplyRendererColor(Color.Lerp(Color.white * rainbowBrightness, Color.white * finalWhiteBrightness, value)),
            1f,
            finalWhiteGlowDuration));
    }

    private void AddSuccessWhiteOverlayTween(Sequence sequence)
    {
        if (sequence == null || successWhiteOverlay == null)
        {
            return;
        }

        ShowOverlay(successWhiteOverlay, 0f);
        sequence.Insert(
            successWhiteFadeDelay,
            DOTween
                .To(
                    () => successWhiteOverlay.color.a,
                    alpha => SetOverlayAlpha(successWhiteOverlay, alpha),
                    1f,
                    successWhiteFadeDuration)
                .SetEase(Ease.InQuad));
    }

    private void ApplyRendererColor(Color color)
    {
        if (rainbowRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer in rainbowRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_EmissionColor", color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void EnsureOverlayImages()
    {
        if (successWhiteOverlay == null)
        {
            successWhiteOverlay = CreateOverlayImage("Success White Overlay", Color.white);
        }

        if (failPoopOverlay == null)
        {
            failPoopOverlay = CreateOverlayImage("Fail Poop Overlay", Color.white);
        }

        if (failPoopSprite != null && failPoopOverlay != null)
        {
            failPoopOverlay.sprite = failPoopSprite;
        }
    }

    private Image CreateOverlayImage(string objectName, Color color)
    {
        GameObject canvasObject = new GameObject(objectName + " Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(canvasObject.transform, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        return image;
    }

    private static void ShowOverlay(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        image.gameObject.SetActive(true);
        SetOverlayAlpha(image, alpha);
    }

    private static void SetOverlayAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private static void HideOverlay(Image image)
    {
        if (image == null)
        {
            return;
        }

        ShowOverlay(image, 0f);
        image.rectTransform.localScale = Vector3.one;
        image.gameObject.SetActive(false);
    }

    private void LoadStartSceneWithReturnMove()
    {
        SceneReturnState.PlayStartReturnCameraMove = true;
        SceneManager.LoadScene(startSceneName);
    }
}
