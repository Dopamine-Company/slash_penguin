using UnityEngine;
using System;

namespace Dopamine.SlashPenguin
{
    public class GameStateMachine : MonoBehaviour
    {
        public static GameStateMachine Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.WaitingStart;

        public event Action<GameState> OnStateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void TransitionTo(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
