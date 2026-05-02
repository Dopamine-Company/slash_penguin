using UnityEngine;

namespace Dopamine.SlashPenguin
{
    public class FartController : MonoBehaviour
    {
        [SerializeField] private PenguinGameSettings _settings;
        [SerializeField] private ParticleSystem _fartParticle;
        [SerializeField] private RednessController _rednessController;

        private int _fartCount;
        private GameStateMachine _stateMachine;

        void Start()
        {
            _stateMachine = GameStateMachine.Instance;
        }

        public void TriggerFart()
        {
            _fartCount++;

            if (_rednessController != null)
                _rednessController.SubtractRedness(_settings.rednessDecrement);

            if (_fartParticle != null)
                _fartParticle.Play();

            if (_fartCount >= _settings.fartThreshold && _stateMachine != null)
                _stateMachine.TransitionTo(GameState.PoopEnd);
        }

        public void ResetFart()
        {
            _fartCount = 0;
        }

        public int FartCount => _fartCount;
    }
}
