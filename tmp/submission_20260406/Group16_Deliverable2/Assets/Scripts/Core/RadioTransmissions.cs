using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using Deadlight.Narrative;

namespace Deadlight.Core
{
    public class RadioTransmissions : MonoBehaviour
    {
        public static RadioTransmissions Instance { get; private set; }

        private Text transmissionText;
        private Image transmissionBg;
        private GameObject transmissionPanel;
        private AudioSource radioAudioSource;
        private AudioClip radioBeepClip;

        private static readonly string[][] nightTransmissions = {
            // Level 1, Night 1
            new[] {
                "[Radio] EVAC Command. Welcome to the zone, medic. Scavenge during daylight — they're slow in the sun.",
                "When the sun sets, they change. Find a defensible position."
            },
            // Level 1, Night 2
            new[] {
                "[Radio] Flight 7 wreckage confirmed. The crash wasn't accidental — something hit it from inside the zone.",
                "Scavenge what you can. The military left supplies when they pulled out."
            },
            // Level 1, Night 3
            new[] {
                "[Radio] Checkpoint logs recovered. Command sealed the district on purpose.",
                "Signals from a research facility. 'Project Lazarus.' We need to keep moving. EVAC out."
            },
            // Level 2, Night 1
            new[] {
                "[Radio] EVAC Command: Suburban corridor is now your active sector.",
                "Shelter and clinic logs here can verify who authorized Lazarus transfers."
            },
            // Level 2, Night 2
            new[] {
                "[Radio] Shelter roster recovered. Families were queued for extraction, then abandoned at the line.",
                "Runner packs are coordinating. Force chokepoints and do not get flanked."
            },
            // Level 2, Night 3
            new[] {
                "[Radio] Final night in the suburbs. Secure the last objective and transmit everything by dawn.",
                "Clinic notes confirm Lazarus patients were hidden in regular evac flow. Finish the upload. EVAC out."
            },
            // Level 3, Night 1
            new[] {
                "[Radio] Urgent. Lazarus files decoded — a black project. Subject 23 was patient zero.",
                "Mutation is accelerating. Exploders and spitters. Keep your distance."
            },
            // Level 3, Night 2
            new[] {
                "[Radio] Command records recovered. The district was quarantined to erase Lazarus, not to save anyone.",
                "The infected are adapting to your tactics. Mix up your approach tonight."
            },
            // Level 3, Night 3
            new[] {
                "[Radio] Lab breach confirmed. Subject 23 was the original host of the networked infection.",
                "The facility is close. One more level after tonight. Don't let them surround you."
            },
            // Level 4, Night 1
            new[] {
                "[Radio] Final level. Military initiated 'Operation Deadlight' — they'll bury everything.",
                "Gate logs show the complex was sealed with live personnel still inside."
            },
            // Level 4, Night 2
            new[] {
                "[Radio] Lazarus archive secured. Subject 23 was weaponized and deployed before the collapse.",
                "Subject 23 is converging. The original host. It gets stronger with each kill."
            },
            // Level 4, Night 3
            new[] {
                "[Radio] This is it. Final night. Beacon is armed in the main lab.",
                "Transmit the data before they destroy it. Helicopter inbound at dawn. Make it count."
            }
        };

        private static readonly string[] dayTips = {
            "TIP: Use buildings as cover. Funnel enemies into narrow kill zones.",
            "TIP: Reload [R] before night falls. An empty gun is a death sentence.",
            "TIP: SHIFT to sprint. Save stamina for when you really need it.",
            "TIP: Exploders damage nearby infected when they pop. Use it.",
            "TIP: The shop opens at dawn. Spend points on weapons and upgrades."
        };

        private static readonly string[] loreMessages = {
            "[Intercepted] Dr. Chen — Day 12: 'Subject 23 shows unprecedented regeneration. Wounds close in seconds.'",
            "[Intercepted] Military Order: 'Project Lazarus is CODE BLACK. All evidence must be destroyed.'",
            "[Intercepted] Field Recording: 'They're not mindless. I watched them coordinate an ambush.'",
            "[Intercepted] Dr. Chen — Final Entry: 'I thought I was saving soldiers. God forgive me.'",
            "[Intercepted] Unknown Signal: 'Subject 23 escaped. It took the others with it. God help us.'",
            "[Intercepted] Quarantine Log: 'Civilian buses turned back at the checkpoint. Orders from above.'",
            "[Intercepted] Pilot Audio: 'Something launched from inside the zone. Flight 7 never had a chance.'",
            "[Intercepted] Lab Tech: 'The regeneration works. But Subject 23 isn't healing — it's growing.'"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            radioAudioSource = gameObject.AddComponent<AudioSource>();
            radioAudioSource.playOnAwake = false;
            radioAudioSource.volume = 0.6f;

            try
            {
                radioBeepClip = Audio.ProceduralAudioGenerator.GeneratePickup();
            }
            catch (System.Exception) { }
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

        private bool TryShowUnifiedMessage(string rawText, float duration, bool interrupt = false, bool playRadioStatic = false)
        {
            var narrative = NarrativeManager.Instance;
            if (narrative == null)
            {
                return false;
            }

            string normalized = NormalizeTransmissionText(rawText);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return true;
            }

            ExtractSpeakerAndMessage(normalized, out string speaker, out string message);
            if (string.IsNullOrWhiteSpace(message))
            {
                message = normalized;
            }

            narrative.QueueSystemMessage(speaker, message, duration, interrupt, playRadioStatic);
            if (transmissionPanel != null)
            {
                transmissionPanel.SetActive(false);
            }

            return true;
        }

        private static void ExtractSpeakerAndMessage(string normalized, out string speaker, out string message)
        {
            speaker = "COMMS";
            message = normalized?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (message.StartsWith("[Intercepted]", StringComparison.OrdinalIgnoreCase))
            {
                speaker = "INTERCEPT";
                message = message.Substring("[Intercepted]".Length).TrimStart(' ', ':', '-').Trim();
            }

            if (message.StartsWith("[PROXIMITY ALERT]", StringComparison.OrdinalIgnoreCase))
            {
                speaker = "ALERT";
                message = message.Substring("[PROXIMITY ALERT]".Length).TrimStart(' ', ':', '-').Trim();
            }

            if (TryStripNamedPrefix(ref message, "EVAC Command"))
            {
                speaker = "EVAC COMMAND";
            }
            else if (TryStripNamedPrefix(ref message, "Command"))
            {
                speaker = "EVAC COMMAND";
            }
            else if (TryStripNamedPrefix(ref message, "Medic"))
            {
                speaker = "MEDIC";
            }
            else if (TryStripNamedPrefix(ref message, "Pilot"))
            {
                speaker = "PILOT";
            }
            else if (message.StartsWith("TIP:", StringComparison.OrdinalIgnoreCase))
            {
                speaker = "GUIDE";
                message = message.Substring(4).Trim();
            }

            if (speaker == "COMMS" && LooksLikeAlert(message))
            {
                speaker = "ALERT";
            }
        }

        private static bool TryStripNamedPrefix(ref string message, string prefix)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(prefix))
            {
                return false;
            }

            if (!message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (message.Length == prefix.Length)
            {
                message = string.Empty;
                return true;
            }

            char marker = message[prefix.Length];
            if (marker != ':' && marker != '.' && marker != '-')
            {
                return false;
            }

            int idx = prefix.Length + 1;
            while (idx < message.Length && char.IsWhiteSpace(message[idx]))
            {
                idx++;
            }

            message = idx < message.Length ? message.Substring(idx).Trim() : string.Empty;
            return true;
        }

        private static bool LooksLikeAlert(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (text.IndexOf("SUBJECT 23", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            int letters = 0;
            int uppercaseLetters = 0;
            foreach (char c in text)
            {
                if (!char.IsLetter(c))
                {
                    continue;
                }

                letters++;
                if (char.IsUpper(c))
                {
                    uppercaseLetters++;
                }
            }

            if (letters < 8)
            {
                return false;
            }

            return uppercaseLetters >= letters * 0.8f;
        }

        private static string NormalizeTransmissionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string normalized = text.Trim();

            if (normalized.StartsWith("[Radio]", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(7).Trim();
            }

            if (normalized.StartsWith("RADIO:", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(6).Trim();
            }

            if (normalized.StartsWith("\"", StringComparison.Ordinal) &&
                normalized.EndsWith("\"", StringComparison.Ordinal) &&
                normalized.Length > 1)
            {
                normalized = normalized.Substring(1, normalized.Length - 2).Trim();
            }

            return normalized;
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
                int level = GameManager.Instance?.CurrentLevel ?? 1;
                int nwl = GameManager.Instance?.NightWithinLevel ?? 1;
                ShowMessage($"LEVEL {level}, NIGHT {nwl} - SURVIVE!", 3f);
            }
            else if (state == GameState.LevelComplete)
            {
                // Level completion details are presented in the dedicated summary panel.
                // Skip transient radio overlays here to avoid fast, redundant congratulation text.
            }
        }

        private IEnumerator PlayTransmissions(int nightIndex)
        {
            yield return new WaitForSeconds(2f);

            if (nightIndex >= 0 && nightIndex < nightTransmissions.Length)
            {
                string[] lines = nightTransmissions[nightIndex];
                foreach (var line in lines)
                {
                    yield return ShowTransmission(line, 4.5f);
                    yield return new WaitForSeconds(3f);
                }
            }
        }

        private IEnumerator ShowTransmission(string text, float duration)
        {
            if (TryShowUnifiedMessage(text, duration))
            {
                yield break;
            }

            if (transmissionPanel == null || transmissionText == null) yield break;

            try
            {
                var staticClip = Audio.ProceduralAudioGenerator.GenerateRadioStatic();
                if (staticClip != null && radioAudioSource != null)
                {
                    radioAudioSource.PlayOneShot(staticClip, 0.35f);
                }
            }
            catch (System.Exception) { }

            if (radioAudioSource != null && radioBeepClip != null)
            {
                radioAudioSource.PlayOneShot(radioBeepClip, 0.4f);
            }

            transmissionText.text = text;
            transmissionPanel.SetActive(true);

            if (transmissionBg != null)
            {
                float fadeIn = 0.3f;
                float elapsed = 0f;
                while (elapsed < fadeIn)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0f, 0.72f, elapsed / fadeIn);
                    transmissionBg.color = new Color(0.02f, 0.03f, 0.04f, alpha);
                    transmissionText.color = new Color(0.95f, 0.95f, 0.9f, elapsed / fadeIn);
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
                    float alpha = Mathf.Lerp(0.72f, 0f, elapsed / fadeOut);
                    transmissionBg.color = new Color(0.02f, 0.03f, 0.04f, alpha);
                    transmissionText.color = new Color(0.95f, 0.95f, 0.9f, 1f - elapsed / fadeOut);
                    yield return null;
                }
            }

            transmissionPanel.SetActive(false);
        }

        private float lastMessageTime = -10f;
        private const float MessageCooldown = 5f;

        public void ShowMessage(string text, float duration)
        {
            if (Time.unscaledTime - lastMessageTime < MessageCooldown)
                return;
            lastMessageTime = Time.unscaledTime;

            if (TryShowUnifiedMessage(text, duration))
            {
                return;
            }

            StartCoroutine(ShowTransmission(text, duration));
        }

        public void ShowRandomLore()
        {
            string lore = loreMessages[UnityEngine.Random.Range(0, loreMessages.Length)];
            StartCoroutine(ShowLoreTransmission(lore));
        }

        private IEnumerator ShowLoreTransmission(string text)
        {
            if (TryShowUnifiedMessage(text, 5f, interrupt: false, playRadioStatic: true))
            {
                yield break;
            }

            if (transmissionPanel == null || transmissionText == null) yield break;

            transmissionText.text = text;
            transmissionPanel.SetActive(true);

            if (transmissionBg != null)
            {
                float fadeIn = 0.5f;
                float elapsed = 0f;
                while (elapsed < fadeIn)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0, 0.9f, elapsed / fadeIn);
                    transmissionBg.color = new Color(0.1f, 0.05f, 0, alpha);
                    transmissionText.color = new Color(1f, 0.8f, 0.3f, elapsed / fadeIn);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(5f);

            if (transmissionBg != null)
            {
                float fadeOut = 0.8f;
                float elapsed = 0f;
                while (elapsed < fadeOut)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(0.9f, 0, elapsed / fadeOut);
                    transmissionBg.color = new Color(0.1f, 0.05f, 0, alpha);
                    transmissionText.color = new Color(1f, 0.8f, 0.3f, 1f - elapsed / fadeOut);
                    yield return null;
                }
            }

            transmissionPanel.SetActive(false);
        }

        public void ShowNightWarning(int night)
        {
            int level = GameManager.GetLevelForNight(night);
            int nwl = GameManager.GetNightWithinLevel(night);
            bool isLevelFinal = GameManager.IsLastNightOfLevel(night);
            bool isGameFinal = night >= GameManager.TotalLevels * GameManager.NightsPerLevel;

            string warning;
            if (isGameFinal)
                warning = "FINAL NIGHT. SUBJECT 23 IS NEAR.";
            else if (isLevelFinal)
                warning = $"LAST NIGHT OF LEVEL {level}. SURVIVE TO ADVANCE.";
            else if (level >= 3)
                warning = "NIGHT FALLS. NEW MUTATIONS INBOUND.";
            else if (nwl >= 2)
                warning = "DARKNESS APPROACHES. RUNNERS DETECTED.";
            else
                warning = "SUNSET. THE INFECTED ARE WAKING.";

            StartCoroutine(ShowWarningTransmission(warning));
        }

        private IEnumerator ShowWarningTransmission(string text)
        {
            if (TryShowUnifiedMessage(text, 3.5f))
            {
                yield break;
            }

            if (transmissionPanel == null || transmissionText == null) yield break;

            try
            {
                var sirenClip = Audio.ProceduralAudioGenerator.GenerateAlarmSiren();
                if (sirenClip != null && radioAudioSource != null)
                {
                    radioAudioSource.PlayOneShot(sirenClip, 0.5f);
                }
            }
            catch (System.Exception) { }

            transmissionText.text = text;
            transmissionPanel.SetActive(true);

            float duration = 3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pulse = (Mathf.Sin(elapsed * 8f) + 1f) * 0.5f;
                
                if (transmissionBg != null)
                {
                    transmissionBg.color = new Color(0.3f, 0, 0, 0.7f + pulse * 0.2f);
                }
                transmissionText.color = new Color(1f, 0.2f + pulse * 0.3f, 0.1f, 1f);
                
                yield return null;
            }

            float fadeOut = 0.5f;
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.9f, 0, elapsed / fadeOut);
                if (transmissionBg != null)
                {
                    transmissionBg.color = new Color(0.3f, 0, 0, alpha);
                }
                transmissionText.color = new Color(1f, 0.3f, 0.1f, 1f - elapsed / fadeOut);
                yield return null;
            }

            transmissionPanel.SetActive(false);
        }

        private bool firstKillPlayed;
        private bool lowHealthPlayed;
        private bool killStreakPlayed;

        public void TriggerFirstKill()
        {
            if (firstKillPlayed) return;
            firstKillPlayed = true;
            ShowMessage("RADIO: First kill confirmed. Don't celebrate — there are thousands more.", 3f);
        }

        public void TriggerLowHealth()
        {
            if (lowHealthPlayed) return;
            lowHealthPlayed = true;
            ShowMessage("RADIO: Survivor, your vitals are critical! Find cover!", 3f);
            lowHealthPlayed = false;
            StartCoroutine(ResetFlagAfterDelay(() => lowHealthPlayed = false, 30f));
        }

        private System.Collections.IEnumerator ResetFlagAfterDelay(System.Action reset, float delay)
        {
            yield return new WaitForSeconds(delay);
            reset?.Invoke();
        }

        public void TriggerKillStreak()
        {
            if (killStreakPlayed) return;
            killStreakPlayed = true;
            ShowMessage("RADIO: Impressive! Command hasn't seen numbers like that since the fall.", 3f);
            StartCoroutine(ResetFlagAfterDelay(() => killStreakPlayed = false, 60f));
        }

        public void TriggerBossHalfHealth()
        {
            ShowMessage("RADIO: It's weakening! Keep up the pressure!", 3f);
        }

        public void TriggerBossDefeated()
        {
            StartCoroutine(PlayVictoryTransmission());
        }

        private IEnumerator PlayVictoryTransmission()
        {
            yield return ShowTransmission("RADIO: \"Biological signature terminated. Subject 23 is down.\"", 4f);
            yield return new WaitForSeconds(1f);
            yield return ShowTransmission("RADIO: \"Lazarus data transmitted. The world will know the truth.\"", 4f);
            yield return new WaitForSeconds(1f);
            yield return ShowTransmission("RADIO: \"Helicopter is two minutes out. Welcome home, medic.\"", 4f);
        }

        public void ShowSubject23Warning()
        {
            StartCoroutine(ShowBossWarning());
        }

        private IEnumerator ShowBossWarning()
        {
            if (TryShowUnifiedMessage("SUBJECT 23 APPROACHES. PREPARE FOR COMBAT.", 4.2f, interrupt: true))
            {
                yield break;
            }

            if (transmissionPanel == null || transmissionText == null) yield break;

            try
            {
                var heartbeatClip = Audio.ProceduralAudioGenerator.GenerateHeartbeat();
                if (heartbeatClip != null && radioAudioSource != null)
                {
                    radioAudioSource.PlayOneShot(heartbeatClip, 0.7f);
                }
            }
            catch (System.Exception) { }

            if (GameEffects.Instance != null)
            {
                GameEffects.Instance.ScreenShake(0.15f, 0.5f);
            }

            string[] bossLines = {
                "[PROXIMITY ALERT]",
                "MASSIVE BIOLOGICAL SIGNATURE DETECTED",
                "SUBJECT 23 APPROACHES",
                "PREPARE FOR COMBAT"
            };

            foreach (var line in bossLines)
            {
                transmissionText.text = line;
                transmissionPanel.SetActive(true);

                if (transmissionBg != null)
                {
                    transmissionBg.color = new Color(0.5f, 0, 0, 0.9f);
                }
                transmissionText.color = new Color(1f, 0.1f, 0.1f, 1f);

                try
                {
                    var heartbeatClip = Audio.ProceduralAudioGenerator.GenerateHeartbeat();
                    if (heartbeatClip != null && radioAudioSource != null)
                    {
                        radioAudioSource.PlayOneShot(heartbeatClip, 0.5f);
                    }
                }
                catch (System.Exception) { }

                yield return new WaitForSeconds(1.5f);
            }

            float fadeOut = 1f;
            float elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.9f, 0, elapsed / fadeOut);
                if (transmissionBg != null)
                {
                    transmissionBg.color = new Color(0.5f, 0, 0, alpha);
                }
                transmissionText.color = new Color(1f, 0.1f, 0.1f, 1f - elapsed / fadeOut);
                yield return null;
            }

            transmissionPanel.SetActive(false);
        }
    }
}
