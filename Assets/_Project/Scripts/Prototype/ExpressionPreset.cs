using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Slash Penguin/Expression Preset")]
public class ExpressionPreset : ScriptableObject
{
    [Serializable]
    public class FaceSpriteState
    {
        public string slotId;
        public Sprite sprite;
        public bool visible = true;
        public GameObject copyFromGameObject;
        [TextArea(2, 5)] public string transformJson;
        public bool useWorldTransform;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;
        public Color color = Color.white;
    }

    [SerializeField] private bool clearUnspecifiedSlots = true;
    [SerializeField] private List<FaceSpriteState> spriteStates = new List<FaceSpriteState>();

    public bool ClearUnspecifiedSlots => clearUnspecifiedSlots;
    public IReadOnlyList<FaceSpriteState> SpriteStates => spriteStates;
}
