using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Deadlight.Core
{
    public class RadioTransmissions : MonoBehaviour
    {
        public static RadioTransmissions Instance { get; private set; }

        private Text transmissionText;
        private Image transmissionBg;
        private GameObject transmissionPanel;

        private static readonly string[][] nightTransmissions = {
            new[] {
                "[Radio crackle] ...this is EVAC Command. Survivor detected in sector 7.",
                "Hold your position. Rescue chopper inbound... ETA: 5 nights.",
                "Scavenge what you can during daylight. The infected are dormant... mostly.",
                "When night falls, they come. Survive until dawn."
            },
            new[] {
                "[Radio] EVAC Command here. You made it through Night 1. Good.",
                "Intel shows the infected are evolving. Expect heavier resistance tonight.",
                "We've detected a weapons cache nearby. Check the shop for a shotgun.",
                "Four more nights, survivor. Keep your head down."
            },
            new[] {
                "[Radio crackle] ...survivor, the situation is deteriorating faster than expected.",
                "New infected types detected: fast-movers and exploders.",
                "The assault rifle should help with the larger waves tonight.",
                "Three nights remaining. The chopper is on schedule."
            },
            new[] {
                "[Static] ...massive infected surge heading your way.",
                "EVAC Command: This is the worst we've seen. Night 4 will test everything.",
                "Use every resource at your disposal. The flamethrower is now available.",
                "Two more nights. You can do this."
            },
            new[] {
                "[Radio] This is it, survivor. Final night.",
                "A massive mutated entity has been detected near your position.",
                "Everything has been leading to this. Use everything you've got.",
                "Survive until dawn and the helicopter will extract you. Good luck."
            }
        };

        private static readonly string[] dayTips = {
            "TIP: Explore the area for health and ammo pickups during the day.",
            "TIP: Use cover behind houses and rocks to avoid zombie attacks.",
            "TIP: Sprint with SHIFT to escape danger, but watch your stamina.",
            "TIP: Reload (R) before entering combat. Don't get caught empty.",
            "TIP: The shop opens at dawn after surviving each night."
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        public void SetUI(Text text, Image bg, GameObject panel)
        {
            transmissionText = text;
            transmissionBg = bg;
            transmissionPanel = panel;
            if (transmissionPanel != null) transmissionPanel.SetActive(false);
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.DayPhase)
            {
                int night = GameManager.Instance?.CurrentNight ?? 1;
                StartCoroutine(PlayTransmissions(night - 1));
            }
            else if (state == GameState.NightPhase)
            {
                int night = GameManager.Instance?.CurrentNight ?? 1;
                ShowMessage($"NIGHT {night} - SURVIVE!", 3f);
            }
        }

        private IEnumerator PlayTransmissions(int nightIndex)
        {
            yield return new WaitForSeconds(1.5f);

            if (nightIndex >= 0 && nightIndex < nightTransmissions.Length)
            {
                string[] lines = nightTransmissions[nightIndex];
                foreach (var line in lines)
                {
                    yield return ShowTransmission(line, 4f);
                    yield return new WaitForSeconds(1f);
                }
            }

            yield return new WaitForSeconds(3f);
            string tip = dayTips[Random.Range(0, dayTips.Length)];
            yield return ShowTransmission(tip, 3f);
        }

        private IEnumerator ShowTransmission(string text, float duration)
        {
            if (transmissionPanel == null || transmissionText == null) yield break;

            transmissionText.text = text;
            transmissionPanel.SetActive(true);

            if (transmissionBg != null)
            {
                float fadeIn = 0.3f;
                float elapsed = 0f;
                while (elapsed < fadeIn)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0, 0.85f, elapsed / fadeIn);
                    transmissionBg.color = new Color(0, 0, 0, alpha);
                    transmissionText.color = new Color(0.3f, 1f, 0.3f, elapsed / fadeIn);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(duration);

            if (transmissionBg != null)
            {
                float fadeOut = 0.5f;
                float elapsed = 0f;
                while (elapsed < fadeOut)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0.85f, 0, elapsed / fadeOut);
                    transmissionBg.color = new Color(0, 0, 0, alpha);
                    transmissionText.color = new Color(0.3f, 1f, 0.3f, 1f - elapsed / fadeOut);
                    yield return null;
                }
            }

            transmissionPanel.SetActive(false);
        }

        public void ShowMessage(string text, float duration)
        {
            StartCoroutine(ShowTransmission(text, duration));
        }
    }
}
