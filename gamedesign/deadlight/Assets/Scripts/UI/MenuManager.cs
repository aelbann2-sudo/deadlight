using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deadlight.Core;
using Deadlight.Systems;

namespace Deadlight.UI
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Main Menu Panel")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Difficulty Selection")]
        [SerializeField] private GameObject difficultyPanel;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button difficultyBackButton;
        [SerializeField] private TextMeshProUGUI difficultyDescriptionText;

        [Header("Pause Menu Panel")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverTitleText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverMenuButton;

        [Header("Victory Panel")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TextMeshProUGUI victoryScoreText;
        [SerializeField] private TextMeshProUGUI victoryStatsText;
        [SerializeField] private Button victoryMenuButton;

        private bool isPaused = false;

        private void Start()
        {
            SetupButtons();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            HideAllPanels();

            if (GameManager.Instance?.CurrentState == GameState.MainMenu)
            {
                ShowMainMenu();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (GameManager.Instance?.CurrentState == GameState.DayPhase ||
                    GameManager.Instance?.CurrentState == GameState.NightPhase)
                {
                    TogglePause();
                }
            }
        }

        private void SetupButtons()
        {
            if (playButton != null)
                playButton.onClick.AddListener(ShowDifficultySelection);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);

            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);

            if (easyButton != null)
                easyButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Easy));

            if (normalButton != null)
                normalButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Normal));

            if (hardButton != null)
                hardButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Hard));

            if (difficultyBackButton != null)
                difficultyBackButton.onClick.AddListener(HideDifficultySelection);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(GoToMainMenu);

            if (retryButton != null)
                retryButton.onClick.AddListener(RestartGame);

            if (gameOverMenuButton != null)
                gameOverMenuButton.onClick.AddListener(GoToMainMenu);

            if (victoryMenuButton != null)
                victoryMenuButton.onClick.AddListener(GoToMainMenu);
        }

        private void HandleGameStateChanged(GameState newState)
        {
            HideAllPanels();

            switch (newState)
            {
                case GameState.MainMenu:
                    ShowMainMenu();
                    break;
                case GameState.GameOver:
                    ShowGameOver();
                    break;
                case GameState.Victory:
                    ShowVictory();
                    break;
            }
        }

        private void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (difficultyPanel != null) difficultyPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }

        public void ShowMainMenu()
        {
            HideAllPanels();
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);

            Time.timeScale = 1f;
            isPaused = false;
        }

        private void ShowDifficultySelection()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);

            if (difficultyPanel != null)
                difficultyPanel.SetActive(true);

            UpdateDifficultyDescription(Difficulty.Normal);
        }

        private void HideDifficultySelection()
        {
            if (difficultyPanel != null)
                difficultyPanel.SetActive(false);

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
        }

        private void SelectDifficulty(Difficulty difficulty)
        {
            GameManager.Instance?.SetDifficulty(difficulty);
            UpdateDifficultyDescription(difficulty);
            StartGame();
        }

        private void UpdateDifficultyDescription(Difficulty difficulty)
        {
            if (difficultyDescriptionText == null) return;

            string description = difficulty switch
            {
                Difficulty.Easy => "Reduced enemy health and damage.\nMore resources available.\nScore multiplier: 0.75x",
                Difficulty.Normal => "The standard survival experience.\nBalanced challenge and resources.\nScore multiplier: 1.0x",
                Difficulty.Hard => "Increased enemy stats.\nScarcer resources.\nScore multiplier: 1.5x",
                _ => ""
            };

            difficultyDescriptionText.text = description;
        }

        private void StartGame()
        {
            HideAllPanels();
            GameManager.Instance?.LoadGameScene();
            GameManager.Instance?.StartNewGame();
        }

        public void TogglePause()
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            isPaused = false;
            HideAllPanels();
            GameManager.Instance?.RestartGame();
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            isPaused = false;
            GameManager.Instance?.ReturnToMainMenu();
        }

        private void ShowGameOver()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            Time.timeScale = 0f;

            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();

                if (gameOverTitleText != null)
                    gameOverTitleText.text = "GAME OVER";

                if (finalScoreText != null)
                    finalScoreText.text = $"Final Score: {stats.finalScore}";

                if (statsText != null)
                {
                    statsText.text = $"Nights Survived: {stats.nightsSurvived}\n" +
                                    $"Enemies Killed: {stats.enemiesKilled}\n" +
                                    $"Points Earned: {stats.totalEarned}\n" +
                                    $"Difficulty: {stats.difficulty}";
                }
            }
        }

        private void ShowVictory()
        {
            if (victoryPanel != null)
                victoryPanel.SetActive(true);

            Time.timeScale = 0f;

            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();

                if (victoryScoreText != null)
                    victoryScoreText.text = $"Final Score: {stats.finalScore}";

                if (victoryStatsText != null)
                {
                    victoryStatsText.text = $"All 5 Nights Survived!\n\n" +
                                           $"Enemies Killed: {stats.enemiesKilled}\n" +
                                           $"Points Earned: {stats.totalEarned}\n" +
                                           $"Difficulty: {stats.difficulty}";
                }
            }
        }

        private void OpenSettings()
        {
            Debug.Log("[MenuManager] Settings not yet implemented");
        }

        private void QuitGame()
        {
            GameManager.Instance?.QuitGame();
        }
    }
}
