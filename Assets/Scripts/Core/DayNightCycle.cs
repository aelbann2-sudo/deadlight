using UnityEngine;
using System;

namespace Deadlight.Core
{
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Time Settings")]
        [SerializeField] private float dayDuration = 180f; // 3 minutes
        [SerializeField] private float nightDuration = 210f; // 3.5 minutes
        [SerializeField] private float transitionDuration = 3f;

        [Header("Lighting")]
        [SerializeField] private Light globalLight;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.8f);
        [SerializeField] private Color nightColor = new Color(0.1f, 0.1f, 0.2f);
        [SerializeField] private Color daySkyColor = new Color(0.5f, 0.7f, 0.9f);
        [SerializeField] private Color nightSkyColor = new Color(0.05f, 0.05f, 0.1f);
        [SerializeField] private float dayIntensity = 1f;
        [SerializeField] private float nightIntensity = 0.3f;

        [Header("Current State")]
        [SerializeField] private float currentTime;
        [SerializeField] private bool isDay = true;
        [SerializeField] private bool isTransitioning = false;

        public float CurrentTime => currentTime;
        public float TimeRemaining => Mathf.Max(0f, isDay ? dayDuration - currentTime : nightDuration - currentTime);
        public float TotalPhaseTime => isDay ? dayDuration : nightDuration;
        public float NormalizedTime => TotalPhaseTime <= 0f ? 0f : currentTime / TotalPhaseTime;
        public bool IsDay => isDay;
        public bool IsNight => !isDay && !isTransitioning;
        public float DayDuration => dayDuration;
        public float NightDuration => nightDuration;

        public event Action OnDayStart;
        public event Action OnNightStart;
        public event Action OnTransitionStart;
        public event Action<float> OnTimeUpdate;

        private bool isPaused = false;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            InitializeLighting();
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
            if (isPaused || isTransitioning) return;

            if (GameManager.Instance?.CurrentState == GameState.DayPhase ||
                GameManager.Instance?.CurrentState == GameState.NightPhase)
            {
                UpdateTime();
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.DayPhase:
                    StartDay();
                    break;
                case GameState.NightPhase:
                    StartNight();
                    break;
                case GameState.DawnPhase:
                case GameState.GameOver:
                case GameState.Victory:
                case GameState.MainMenu:
                    isPaused = true;
                    break;
            }
        }

        private void InitializeLighting()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (globalLight == null)
            {
                var existingLight = FindObjectOfType<Light>();
                if (existingLight != null)
                {
                    globalLight = existingLight;
                }
            }

            SetDayLighting();
        }

        private void UpdateTime()
        {
            currentTime += Time.deltaTime;
            OnTimeUpdate?.Invoke(TimeRemaining);

            float targetDuration = isDay ? dayDuration : nightDuration;

            if (currentTime >= targetDuration)
            {
                OnPhaseComplete();
            }
        }

        private void OnPhaseComplete()
        {
            if (isDay)
            {
                StartTransitionToNight();
            }
            else
            {
                GameManager.Instance?.OnNightSurvived();
            }
        }

        public void StartDay()
        {
            isDay = true;
            currentTime = 0f;
            isPaused = false;

            SetDayLighting();
            OnDayStart?.Invoke();

            Debug.Log("[DayNightCycle] Day phase started");
        }

        public void StartNight()
        {
            isDay = false;
            currentTime = 0f;
            isPaused = false;

            SetNightLighting();
            OnNightStart?.Invoke();

            Debug.Log("[DayNightCycle] Night phase started");
        }

        private void StartTransitionToNight()
        {
            isTransitioning = true;
            OnTransitionStart?.Invoke();
            StartCoroutine(TransitionToNightCoroutine());
        }

        private System.Collections.IEnumerator TransitionToNightCoroutine()
        {
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;

                if (globalLight != null)
                {
                    globalLight.color = Color.Lerp(dayColor, nightColor, t);
                    globalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
                }

                if (mainCamera != null)
                {
                    mainCamera.backgroundColor = Color.Lerp(daySkyColor, nightSkyColor, t);
                }

                yield return null;
            }

            isTransitioning = false;
            GameManager.Instance?.StartNightPhase();
        }

        private void SetDayLighting()
        {
            if (globalLight != null)
            {
                globalLight.color = dayColor;
                globalLight.intensity = dayIntensity;
            }

            if (mainCamera != null)
            {
                mainCamera.backgroundColor = daySkyColor;
            }
        }

        private void SetNightLighting()
        {
            if (globalLight != null)
            {
                globalLight.color = nightColor;
                globalLight.intensity = nightIntensity;
            }

            if (mainCamera != null)
            {
                mainCamera.backgroundColor = nightSkyColor;
            }
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        public void AdjustDayDuration(float multiplier)
        {
            dayDuration *= multiplier;
        }

        public void AdjustNightDuration(float multiplier)
        {
            nightDuration *= multiplier;
        }

        public void SetDayDuration(float seconds)
        {
            dayDuration = Mathf.Max(5f, seconds);
        }

        public void SetNightDuration(float seconds)
        {
            nightDuration = Mathf.Max(5f, seconds);
        }
    }
}
