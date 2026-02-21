using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Deadlight.Core;
using Deadlight.Player;
using Deadlight.Systems;

namespace Deadlight.Narrative
{
    public class PlayerVoice : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float cooldownDuration = 8f;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float floatSpeed = 0.5f;

        private Canvas voiceCanvas;
        private Text voiceText;
        private RectTransform voiceRect;
        private Font font;

        private float lastVoiceTime = -100f;
        private Coroutine activePopup;

        private PlayerHealth playerHealth;
        private PlayerShooting playerShooting;
        private float previousHealth;
        private int killCount;
        private int killStreakCounter;
        private float killStreakWindowEnd;

        private static readonly string[] gameStartLines =
        {
            "Five nights. I can do this.",
            "Stay focused. Help is coming."
        };

        private static readonly string[] reloadLines =
        {
            "Reloading!",
            "Cover me!",
            "Changing mag!"
        };

        private static readonly string[] lowAmmoLines =
        {
            "Running low...",
            "Need ammo.",
            "Almost out."
        };

        private static readonly string[] killStreakLines =
        {
            "That's right!",
            "Come on!",
            "Keep 'em coming!"
        };

        private static readonly string[] takingDamageLines =
        {
            "Gah!",
            "I'm hit!",
            "Need to be more careful."
        };

        private static readonly string[] lowHealthLines =
        {
            "Not yet...",
            "Stay focused...",
            "Can't stop now."
        };

        private static readonly string[] healingLines =
        {
            "That's better.",
            "Back in the fight."
        };

        private static readonly string[] nightStartLines =
        {
            "Here they come.",
            "Let's do this.",
            "Stay sharp."
        };

        private static readonly string[] dawnLines =
        {
            "Made it.",
            "One more night down.",
            "Thank God..."
        };

        private void Start()
        {
            LoadFont();
            BuildWorldCanvas();
            StartCoroutine(BindEventsDelayed());
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        private void LoadFont()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (font == null)
            {
                string[] installed = Font.GetOSInstalledFontNames();
                if (installed != null && installed.Length > 0)
                    font = Font.CreateDynamicFontFromOSFont(installed[0], 14);
            }
        }

        private void BuildWorldCanvas()
        {
            var canvasObj = new GameObject("VoiceCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            voiceCanvas = canvasObj.AddComponent<Canvas>();
            voiceCanvas.renderMode = RenderMode.WorldSpace;
            voiceCanvas.sortingOrder = 50;

            voiceRect = canvasObj.GetComponent<RectTransform>();
            voiceRect.sizeDelta = new Vector2(4f, 1f);
            voiceRect.localScale = new Vector3(0.02f, 0.02f, 0.02f);

            var textObj = new GameObject("VoiceText");
            textObj.transform.SetParent(canvasObj.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            voiceText = textObj.AddComponent<Text>();
            voiceText.font = font;
            voiceText.fontSize = 24;
            voiceText.alignment = TextAnchor.MiddleCenter;
            voiceText.color = new Color(1f, 1f, 1f, 0f);
            voiceText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            voiceText.text = "";
        }

        private IEnumerator BindEventsDelayed()
        {
            yield return new WaitForSeconds(0.2f);
            SubscribeAll();
        }

        private void SubscribeAll()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                playerShooting = player.GetComponent<PlayerShooting>();
            }

            if (playerHealth != null)
            {
                previousHealth = playerHealth.CurrentHealth;
                playerHealth.OnDamageTaken += OnDamageTaken;
                playerHealth.OnHealthChanged += OnHealthChanged;
            }

            if (playerShooting != null)
            {
                playerShooting.OnReloadStarted += OnReloadStarted;
                playerShooting.OnLowAmmoWarning += OnLowAmmo;
                playerShooting.OnWeaponFired += OnWeaponFired;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }

            if (PointsSystem.Instance != null)
            {
                PointsSystem.Instance.OnPointsEarned += OnPointsEarned;
            }
        }

        private void UnsubscribeAll()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDamageTaken -= OnDamageTaken;
                playerHealth.OnHealthChanged -= OnHealthChanged;
            }

            if (playerShooting != null)
            {
                playerShooting.OnReloadStarted -= OnReloadStarted;
                playerShooting.OnLowAmmoWarning -= OnLowAmmo;
                playerShooting.OnWeaponFired -= OnWeaponFired;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }

            if (PointsSystem.Instance != null)
            {
                PointsSystem.Instance.OnPointsEarned -= OnPointsEarned;
            }
        }

        private void OnDamageTaken(float amount)
        {
            if (playerHealth == null) return;

            float healthPct = playerHealth.CurrentHealth / Mathf.Max(1f, playerHealth.MaxHealth);

            if (healthPct <= 0.25f)
            {
                TryShowLine(lowHealthLines);
            }
            else
            {
                TryShowLine(takingDamageLines);
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (current > previousHealth && previousHealth > 0f)
            {
                TryShowLine(healingLines);
            }
            previousHealth = current;
        }

        private void OnReloadStarted()
        {
            TryShowLine(reloadLines);
        }

        private void OnLowAmmo(int current, int reserve)
        {
            TryShowLine(lowAmmoLines);
        }

        private void OnWeaponFired()
        {
            if (Time.time < killStreakWindowEnd)
            {
                killStreakCounter++;
            }
            else
            {
                killStreakCounter = 0;
            }
        }

        private void OnPointsEarned(int amount, string source)
        {
            if (source == null) return;

            bool isKill = source.Contains("Kill") || source.Contains("kill") || source.Contains("Enemy");
            if (!isKill) return;

            killStreakCounter++;
            killStreakWindowEnd = Time.time + 3f;

            if (killStreakCounter >= 3)
            {
                TryShowLine(killStreakLines);
                killStreakCounter = 0;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.DayPhase:
                    int night = GameManager.Instance != null ? GameManager.Instance.CurrentNight : 1;
                    if (night == 1)
                    {
                        TryShowLine(gameStartLines, true);
                    }
                    else
                    {
                        TryShowLine(dawnLines, true);
                    }
                    break;

                case GameState.NightPhase:
                    TryShowLine(nightStartLines, true);
                    break;
            }
        }

        private void TryShowLine(string[] lines, bool ignoreCooldown = false)
        {
            if (lines == null || lines.Length == 0) return;
            if (!ignoreCooldown && Time.unscaledTime - lastVoiceTime < cooldownDuration) return;

            string line = lines[Random.Range(0, lines.Length)];
            ShowPopup(line);
        }

        private void ShowPopup(string text)
        {
            lastVoiceTime = Time.unscaledTime;

            if (activePopup != null)
            {
                StopCoroutine(activePopup);
            }

            activePopup = StartCoroutine(PopupRoutine(text));
        }

        private IEnumerator PopupRoutine(string text)
        {
            if (voiceText == null || voiceRect == null) yield break;

            voiceRect.localPosition = new Vector3(0f, 1.2f, 0f);
            voiceText.text = text;
            voiceText.color = Color.white;

            float elapsed = 0f;
            float fadeInDuration = 0.2f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                voiceText.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            voiceText.color = Color.white;

            float holdTime = displayDuration - 0.2f - 0.6f;
            elapsed = 0f;
            while (elapsed < holdTime)
            {
                elapsed += Time.deltaTime;
                float yOffset = 1.2f + (floatSpeed * elapsed);
                voiceRect.localPosition = new Vector3(0f, yOffset, 0f);
                yield return null;
            }

            float fadeOutDuration = 0.6f;
            float fadeStart = voiceRect.localPosition.y;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                voiceText.color = new Color(1f, 1f, 1f, alpha);
                float yOffset = fadeStart + (floatSpeed * elapsed);
                voiceRect.localPosition = new Vector3(0f, yOffset, 0f);
                yield return null;
            }

            voiceText.color = new Color(1f, 1f, 1f, 0f);
            voiceText.text = "";
            activePopup = null;
        }
    }
}
