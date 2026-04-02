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
        private AudioSource radioAudioSource;
        private AudioClip radioBeepClip;

        private static readonly string[][] nightTransmissions = {
            // Level 1 — Town Center: "First Light"
            new[] {
                "[Radio crackle] EVAC Command to ground. Welcome to your first day in the zone, medic.",
                "Flight 7's wreckage is nearby. Something brought that bird down from inside the perimeter.",
                "During daylight the infected are slow. Scavenge what you can — ammo, medical supplies, anything.",
                "When the sun sets, they change. Faster. Angrier. Find a defensible position.",
                "We're picking up faint signals from a research facility to the north. 'Project Lazarus.'",
                "Reach it in four days and transmit the data. That's your ticket home.",
                "First things first: survive tonight. EVAC Command out."
            },
            // Level 2 — Suburban: "No One Left Behind"
            new[] {
                "[Radio] Good work making it through Level 1, medic.",
                "You're entering the suburbs now. This neighborhood was on the evacuation route.",
                "Keyword: was. The military sealed the quarantine line before the buses cleared out.",
                "We intercepted partial files from a Dr. Chen — lead researcher on Project Lazarus.",
                "She was working on cellular regeneration. Making soldiers that couldn't die.",
                "Whatever she built, it's still spreading. The infected are evolving.",
                "Watch for runners. They don't shamble — they hunt. Two levels left. Stay sharp."
            },
            // Level 3 — Industrial: "The Source"
            new[] {
                "[Radio crackle] Urgent transmission, medic.",
                "We decoded the Lazarus files. It was a military black project — immortal soldiers.",
                "Subject 23 was the breakthrough. Perfect regeneration. Perfect weapon.",
                "Then it escaped containment three weeks ago. Patient zero for everything you see out there.",
                "The mutation is accelerating. Exploders — bodies so unstable they detonate on death.",
                "Spitters — ranged acid projectors. Keep your distance from both.",
                "The research facility is close. One more level after tonight.",
                "Don't let them surround you. EVAC Command out."
            },
            // Level 4 — Research: "Operation Deadlight"
            new[] {
                "[Radio] Medic. This is the final level.",
                "We've intercepted military comms. They've initiated 'Operation Deadlight.'",
                "The order is to destroy the facility and bury every trace of Project Lazarus.",
                "We need that data transmitted before they succeed. The world deserves the truth.",
                "Massive biological signature moving toward your position.",
                "It's Subject 23. The original host. The source of all of this.",
                "Dr. Chen's last entry says it absorbed every test subject. It gets stronger with each kill.",
                "This thing ended the world, medic. Tonight, you end it.",
                "Helicopter is inbound. Hold until dawn.",
                "For everyone we've lost. Make it count. EVAC Command... we believe in you."
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
                ShowMessage($"LEVEL {night} - SURVIVE!", 3f);
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

        public void ShowMessage(string text, float duration)
        {
            StartCoroutine(ShowTransmission(text, duration));
        }

        public void ShowRandomLore()
        {
            string lore = loreMessages[Random.Range(0, loreMessages.Length)];
            StartCoroutine(ShowLoreTransmission(lore));
        }

        private IEnumerator ShowLoreTransmission(string text)
        {
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
            string[] warnings = {
                "SUNSET. THE INFECTED ARE WAKING.",
                "DARKNESS APPROACHES. RUNNERS DETECTED.",
                "NIGHT FALLS. NEW MUTATIONS INBOUND.",
                "FINAL NIGHT. SUBJECT 23 IS NEAR."
            };

            int index = Mathf.Clamp(night - 1, 0, warnings.Length - 1);
            StartCoroutine(ShowWarningTransmission(warnings[index]));
        }

        private IEnumerator ShowWarningTransmission(string text)
        {
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
