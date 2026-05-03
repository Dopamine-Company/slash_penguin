using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FaceSpriteSlot : MonoBehaviour
{
    [SerializeField] private string slotId;
    [SerializeField] private FaceAnchor anchor;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public string SlotId => slotId;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (anchor != null)
        {
            transform.SetParent(anchor.transform, false);
        }
    }

    public void Apply(Sprite sprite, bool visible, bool useWorldTransform, Vector3 position, Vector3 eulerAngles, Vector3 scale, Color color)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.enabled = visible && sprite != null;

        if (useWorldTransform)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(eulerAngles));
            SetWorldScale(scale);
            return;
        }

        transform.localPosition = position;
        transform.localRotation = Quaternion.Euler(eulerAngles);
        transform.localScale = scale;
    }

    public void Hide()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        spriteRenderer.enabled = false;
    }

    private void SetWorldScale(Vector3 worldScale)
    {
        if (transform.parent == null)
        {
            transform.localScale = worldScale;
            return;
        }

        Vector3 parentScale = transform.parent.lossyScale;
        transform.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
    }
}
