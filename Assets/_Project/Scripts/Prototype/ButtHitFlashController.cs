using System.Collections;
using UnityEngine;

public class ButtHitFlashController : MonoBehaviour
{
    [Header("Game Loop")]
    [SerializeField] private GameLoopController gameLoopController;

    [Header("Spheres")]
    [SerializeField] private JellySwipeSphere leftSwipeSphere;
    [SerializeField] private JellySwipeSphere rightSwipeSphere;

    [Header("Anchors")]
    [SerializeField] private Transform leftButtAnchor;
    [SerializeField] private Transform rightButtAnchor;
    [SerializeField] private Camera targetCamera;

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, -0.7f);
    [SerializeField] private int sortingOrder = 10;

    private SpriteRenderer _leftRenderer;
    private SpriteRenderer _rightRenderer;
    private Coroutine _leftCoroutine;
    private Coroutine _rightCoroutine;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _leftRenderer = CreateFlashRenderer(leftButtAnchor, "Left Hit Flash");
        _rightRenderer = CreateFlashRenderer(rightButtAnchor, "Right Hit Flash");
    }

    private void OnEnable()
    {
        if (leftSwipeSphere != null) leftSwipeSphere.onPhysicalContact.AddListener(HandleLeftContact);
        if (rightSwipeSphere != null) rightSwipeSphere.onPhysicalContact.AddListener(HandleRightContact);
    }

    private void OnDisable()
    {
        if (leftSwipeSphere != null) leftSwipeSphere.onPhysicalContact.RemoveListener(HandleLeftContact);
        if (rightSwipeSphere != null) rightSwipeSphere.onPhysicalContact.RemoveListener(HandleRightContact);
    }

    private void HandleLeftContact()
    {
        FaceCamera();
        TriggerFlash(ref _leftCoroutine, _leftRenderer);
    }

    private void HandleRightContact()
    {
        FaceCamera();
        TriggerFlash(ref _rightCoroutine, _rightRenderer);
    }

    private void TriggerFlash(ref Coroutine coroutine, SpriteRenderer sr)
    {
        if (sr == null) return;
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(FlashRoutine(sr));
    }

    private IEnumerator FlashRoutine(SpriteRenderer sr)
    {
        sr.color = flashColor;
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / flashDuration);
            sr.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }
        sr.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }

    private void FaceCamera()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;
        if (_leftRenderer != null) _leftRenderer.transform.rotation = cam.transform.rotation;
        if (_rightRenderer != null) _rightRenderer.transform.rotation = cam.transform.rotation;
    }

    private SpriteRenderer CreateFlashRenderer(Transform anchor, string rendererName)
    {
        if (anchor == null) return null;

        GameObject go = new GameObject(rendererName);
        go.transform.SetParent(anchor, false);
        go.transform.localPosition = localOffset;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateCircleSprite(128);
        sr.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    private static Sprite GenerateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        Vector2 center = Vector2.one * (size * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), center) <= radius
                    ? Color.white : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }
}
