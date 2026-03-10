using UnityEngine;

namespace DeepShift.Core
{
    public enum GameState
    {
        MainMenu,
        SurfaceCamp,
        InShift,
        PostShift,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState previous = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameManager] State: {previous} → {newState}");
        }
    }
}
