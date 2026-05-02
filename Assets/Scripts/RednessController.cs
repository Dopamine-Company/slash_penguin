using UnityEngine;

namespace Dopamine.SlashPenguin
{
    public class RednessController : MonoBehaviour
    {
        [SerializeField] private Renderer _leftButtockRenderer;
        [SerializeField] private Renderer _rightButtockRenderer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private float _redness;
        private MaterialPropertyBlock _mpbLeft;
        private MaterialPropertyBlock _mpbRight;
        private GameStateMachine _stateMachine;

        void Start()
        {
            _stateMachine = GameStateMachine.Instance;
            _mpbLeft = new MaterialPropertyBlock();
            _mpbRight = new MaterialPropertyBlock();
            ApplyRedness();
        }

        public void AddRedness(float amount)
        {
            _redness = Mathf.Clamp01(_redness + amount);
            ApplyRedness();
            if (_redness >= 1f && _stateMachine != null)
                _stateMachine.TransitionTo(GameState.SuccessEnd);
        }

        public void SubtractRedness(float amount)
        {
            _redness = Mathf.Clamp01(_redness - amount);
            ApplyRedness();
        }

        private void ApplyRedness()
        {
            Color baseColor = Color.Lerp(Color.white, Color.red, _redness);
            Color emissionColor = _redness >= 1f ? Color.red * 2f : Color.black;

            SetBlock(_leftButtockRenderer, _mpbLeft, baseColor, emissionColor);
            SetBlock(_rightButtockRenderer, _mpbRight, baseColor, emissionColor);
        }

        private void SetBlock(Renderer r, MaterialPropertyBlock mpb, Color baseColor, Color emissionColor)
        {
            if (r == null) return;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, baseColor);
            mpb.SetColor(EmissionColorId, emissionColor);
            r.SetPropertyBlock(mpb);
        }

        public void ResetRedness()
        {
            _redness = 0f;
            ApplyRedness();
        }

        public float Redness => _redness;
    }
}
