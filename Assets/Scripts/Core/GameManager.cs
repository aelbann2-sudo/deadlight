using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Deadlight.Level;
using Deadlight.Narrative;

namespace Deadlight.Core
{
    public enum GameState
    {
        MainMenu,
        DayPhase,
        Transition,
        NightPhase,
        DawnPhase,
        GameOver,
        Victory
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private DifficultySettings easySettings;
        [SerializeField] private DifficultySettings normalSettings;
        [SerializeField] private DifficultySettings hardSettings;

        [Header("Current State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private Difficulty currentDifficulty = Difficulty.Normal;
        [SerializeField] private int currentNight = 1;
        [SerializeField] private int maxNights = 5;

        public GameState CurrentState => currentState;
        public Difficulty CurrentDifficulty => currentDifficulty;
        public int CurrentNight => currentNight;
        public int MaxNights => maxNights;
        public DifficultySettings CurrentSettings => GetDifficultySettings();

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnNightChanged;

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

        public DifficultySettings GetDifficultySettings()
        {
            return currentDifficulty switch
            {
                Difficulty.Easy => easySettings,
                Difficulty.Normal => normalSettings,
                Difficulty.Hard => hardSettings,
                _ => normalSettings
            };
        }

        public void SetDifficulty(Difficulty difficulty)
        {
            currentDifficulty = difficulty;
        }

        public void StartNewGame()
        {
            currentNight = 1;
            
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ResetAllSpawnPoints();
            }

            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.ResetPlayedDialogues();
                NarrativeManager.Instance.TriggerDialogue(DialogueTriggerType.GameStart, currentNight);
            }

            ChangeState(GameState.DayPhase);
            OnNightChanged?.Invoke(currentNight);
        }

        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnGameStateChanged?.Invoke(newState);

            Debug.Log($"[GameManager] State changed to: {newState}");
        }

        public void StartNightPhase()
        {
            ChangeState(GameState.NightPhase);
        }

        public void OnNightSurvived()
        {
            if (currentNight >= maxNights)
            {
                ChangeState(GameState.Victory);
            }
            else
            {
                ChangeState(GameState.DawnPhase);
            }
        }

        public void AdvanceToNextNight()
        {
            currentNight++;
            OnNightChanged?.Invoke(currentNight);
            ChangeState(GameState.DayPhase);
        }

        public void OnPlayerDeath()
        {
            ChangeState(GameState.GameOver);
        }

        public void ReturnToMainMenu()
        {
            currentNight = 1;
            ChangeState(GameState.MainMenu);
            SceneManager.LoadScene("MainMenu");
        }

        public void RestartGame()
        {
            currentNight = 1;
            SceneManager.LoadScene("Game");
            ChangeState(GameState.DayPhase);
            OnNightChanged?.Invoke(currentNight);
        }

        public void LoadGameScene()
        {
            SceneManager.LoadScene("Game");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
