using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ButtGlowSpriteController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform[] glowAnchors;
    [SerializeField] private Camera targetCamera;

    [Header("Sprite")]
    [SerializeField] private Sprite glowSprite;
    [SerializeField] private bool generateGlowSpriteIfMissing = true;
    [SerializeField] private int generatedTextureSize = 128;
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Header("Look")]
    [SerializeField] private float baseScale = 1.15f;
    [SerializeField] private float pulseScale = 1.65f;
    [SerializeField] private float maxAlpha = 0.85f;
    [SerializeField] private float colorBrightness = 2.2f;
    [SerializeField] private float finalWhiteBrightness = 4.5f;

    [Header("Timing")]
    [SerializeField] private float rainbowDuration = 1.25f;
    [SerializeField] private float rainbowPulseInterval = 0.09f;
    [SerializeField] private float finalWhiteDuration = 0.55f;

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

    private readonly List<SpriteRenderer> glowRenderers = new List<SpriteRenderer>();
    private Sequence currentSequence;
    private Sprite generatedSprite;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        EnsureGlowRenderers();
        SetGlowVisible(false);
    }

    public Sequence PlayGlow()
    {
        EnsureGlowRenderers();
        FaceCamera();

        currentSequence?.Kill();
        ResetGlowRenderers();
        SetGlowVisible(true);

        currentSequence = DOTween.Sequence();

        float elapsed = 0f;
        int colorIndex = 0;

        while (elapsed < rainbowDuration)
        {
            Color color = rainbowColors[colorIndex % rainbowColors.Length] * colorBrightness;
            float alpha = maxAlpha * Mathf.Lerp(0.65f, 1f, Mathf.PingPong(colorIndex * 0.42f, 1f));
            float scale = Mathf.Lerp(baseScale, pulseScale, Mathf.PingPong(colorIndex * 0.5f, 1f));

            currentSequence.InsertCallback(elapsed, () => ApplyGlow(color, alpha, scale));

            elapsed += Mathf.Max(0.01f, rainbowPulseInterval);
            colorIndex++;
        }

        currentSequence.Insert(rainbowDuration, DOTween.To(
            () => 0f,
            value =>
            {
                Color color = Color.white * Mathf.Lerp(colorBrightness, finalWhiteBrightness, value);
                float alpha = Mathf.Lerp(maxAlpha, 1f, value);
                float scale = Mathf.Lerp(pulseScale, pulseScale * 1.3f, value);
                ApplyGlow(color, alpha, scale);
            },
            1f,
            finalWhiteDuration));

        return currentSequence;
    }

    public void HideGlow()
    {
        currentSequence?.Kill();
        SetGlowVisible(false);
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
        currentSequence = null;
    }

    private void EnsureGlowRenderers()
    {
        if (glowRenderers.Count > 0)
        {
            return;
        }

        Sprite sprite = GetGlowSprite();
        if (sprite == null || glowAnchors == null)
        {
            return;
        }

        foreach (Transform anchor in glowAnchors)
        {
            if (anchor == null)
            {
                continue;
            }

            GameObject glowObject = new GameObject(anchor.name + " Glow Sprite");
            glowObject.transform.SetParent(anchor, false);
            glowObject.transform.localPosition = localOffset;
            glowObject.transform.localScale = Vector3.one * baseScale;

            SpriteRenderer spriteRenderer = glowObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.color = Transparent(Color.white);

            glowRenderers.Add(spriteRenderer);
        }
    }

    private Sprite GetGlowSprite()
    {
        if (glowSprite != null)
        {
            return glowSprite;
        }

        if (!generateGlowSpriteIfMissing)
        {
            return null;
        }

        if (generatedSprite == null)
        {
            generatedSprite = GenerateRadialGlowSprite(Mathf.Max(16, generatedTextureSize));
        }

        return generatedSprite;
    }

    private static Sprite GenerateRadialGlowSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Generated Butt Glow";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                alpha *= alpha;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
    }

    private void ResetGlowRenderers()
    {
        foreach (SpriteRenderer spriteRenderer in glowRenderers)
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            spriteRenderer.transform.localScale = Vector3.one * baseScale;
            spriteRenderer.color = Transparent(Color.white);
        }
    }

    private void ApplyGlow(Color color, float alpha, float scale)
    {
        foreach (SpriteRenderer spriteRenderer in glowRenderers)
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            spriteRenderer.color = WithAlpha(color, alpha);
            spriteRenderer.transform.localScale = Vector3.one * scale;
        }
    }

    private void SetGlowVisible(bool visible)
    {
        foreach (SpriteRenderer spriteRenderer in glowRenderers)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.gameObject.SetActive(visible);
            }
        }
    }

    private void FaceCamera()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            return;
        }

        foreach (SpriteRenderer spriteRenderer in glowRenderers)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.transform.rotation = cam.transform.rotation;
            }
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color Transparent(Color color)
    {
        color.a = 0f;
        return color;
    }
}
