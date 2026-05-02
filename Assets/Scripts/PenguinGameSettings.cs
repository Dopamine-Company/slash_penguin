using UnityEngine;

namespace Dopamine.SlashPenguin
{
    [CreateAssetMenu(fileName = "PenguinGameSettings", menuName = "SlashPenguin/Game Settings")]
    public class PenguinGameSettings : ScriptableObject
    {
        [Header("Panty")]
        public float pantyPullThreshold = -1.5f;
        public float pantySwipeSensitivity = 0.005f;

        [Header("Buttock")]
        public float buttockActiveDuration = 1.5f;
        public float buttockCooldownDuration = 0.5f;
        public float buttockScaleMultiplier = 1.4f;
        public float buttockScaleDuration = 0.3f;

        [Header("Redness")]
        public float rednessIncrement = 0.2f;
        public float rednessDecrement = 0.1f;

        [Header("Fart")]
        public int fartThreshold = 4;
        public float poopCoverDuration = 1.0f;

        [Header("Input")]
        public float swipeMinDistance = 50f;
    }
}
