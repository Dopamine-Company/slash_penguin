using UnityEngine;

public class MouseTrailController : MonoBehaviour
{
    [Header("Trail")]
    [SerializeField] private Color trailColor = Color.white;
    [SerializeField] private float trailTime = 0.3f;
    [SerializeField] private float trailWidth = 0.1f;

    [Header("Position")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float worldDepth = 4f;

    private Transform _trailTransform;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _trailTransform = CreateTrailObject();
    }

    private void Update()
    {
        if (targetCamera == null) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = worldDepth;
        _trailTransform.position = targetCamera.ScreenToWorldPoint(mousePos);
    }

    private Transform CreateTrailObject()
    {
        GameObject go = new GameObject("Mouse Trail");
        go.transform.SetParent(transform);

        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.05f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(trailColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
        trail.material = new Material(Shader.Find("Sprites/Default"));

        return go.transform;
    }
}
