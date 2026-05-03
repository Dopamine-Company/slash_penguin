using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpressionController : MonoBehaviour
{
    [SerializeField] private ExpressionPreset defaultPersistentPreset;
    [SerializeField] private List<FaceSpriteSlot> slots = new List<FaceSpriteSlot>();
    [SerializeField] private bool autoFindChildSlots = true;

    private readonly Dictionary<string, FaceSpriteSlot> slotById = new Dictionary<string, FaceSpriteSlot>();
    private ExpressionPreset currentPersistentPreset;
    private Coroutine transientRoutine;

    private void Awake()
    {
        RebuildSlotLookup();
        ApplyPersistent(defaultPersistentPreset);
    }

    private void OnValidate()
    {
        RemoveMissingSlotReferences();
    }

    public void ApplyPersistent(ExpressionPreset preset)
    {
        currentPersistentPreset = preset;
        ApplyPreset(preset);
    }

    public void PlayTransient(ExpressionPreset preset, float duration)
    {
        if (transientRoutine != null)
        {
            StopCoroutine(transientRoutine);
        }

        transientRoutine = StartCoroutine(TransientRoutine(preset, duration));
    }

    public void RebuildSlotLookup()
    {
        if (autoFindChildSlots)
        {
            slots.Clear();
            slots.AddRange(GetComponentsInChildren<FaceSpriteSlot>(true));
        }

        slotById.Clear();
        RemoveMissingSlotReferences();

        for (int i = 0; i < slots.Count; i++)
        {
            FaceSpriteSlot slot = slots[i];

            if (slot == null || string.IsNullOrEmpty(slot.SlotId))
            {
                continue;
            }

            slotById[slot.SlotId] = slot;
        }
    }

    private IEnumerator TransientRoutine(ExpressionPreset preset, float duration)
    {
        ApplyPreset(preset);
        yield return new WaitForSeconds(duration);
        ApplyPreset(currentPersistentPreset);
        transientRoutine = null;
    }

    private void ApplyPreset(ExpressionPreset preset)
    {
        if (preset == null)
        {
            HideAllSlots();
            return;
        }

        if (preset.ClearUnspecifiedSlots)
        {
            HideAllSlots();
        }

        IReadOnlyList<ExpressionPreset.FaceSpriteState> states = preset.SpriteStates;

        for (int i = 0; i < states.Count; i++)
        {
            ExpressionPreset.FaceSpriteState state = states[i];

            if (state == null || string.IsNullOrEmpty(state.slotId))
            {
                continue;
            }

            if (!slotById.TryGetValue(state.slotId, out FaceSpriteSlot slot) || slot == null)
            {
                continue;
            }

            slot.Apply(
                state.sprite,
                state.visible,
                state.useWorldTransform,
                state.localPosition,
                state.localEulerAngles,
                state.localScale,
                state.color);
        }
    }

    private void HideAllSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].Hide();
            }
        }
    }

    private void RemoveMissingSlotReferences()
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i] == null)
            {
                slots.RemoveAt(i);
            }
        }
    }
}
