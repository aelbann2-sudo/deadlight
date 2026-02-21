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
            // Night 1 - Introduction & First Hints of Project Lazarus
            new[] {
                "[Radio crackle] ...this is EVAC Command. We've detected a survivor in the quarantine zone.",
                "Survivor, do you copy? Extraction team is en route. ETA: five nights.",
                "The convoy left without you. We're sorry. But we will get you out.",
                "During daylight, the infected are sluggish. Scavenge supplies. Stay quiet.",
                "At night... they become aggressive. Find shelter. Defend your position.",
                "One more thing... we've intercepted unusual transmissions from the old research facility.",
                "Something about 'Project Lazarus'. Stay away from that area if you can.",
                "Survive until dawn, soldier. EVAC Command out."
            },
            // Night 2 - Hints of Dr. Chen, Evolution of Infected
            new[] {
                "[Radio] EVAC Command checking in. Outstanding work surviving Night 1.",
                "Bad news: our scouts report the infected are... changing.",
                "They're faster. More coordinated. This isn't normal pathogen behavior.",
                "We found partial records from the research facility. Something about a Dr. Chen.",
                "She was lead researcher on... [static] ...cellular regeneration project.",
                "Whatever she created, it's spreading. The infected are adapting.",
                "A shotgun was found in a supply drop. Check the shop at dawn.",
                "Watch for runners. They don't shamble, survivor. They hunt.",
                "Four nights to extraction. Stay sharp. EVAC Command out."
            },
            // Night 3 - Revelation: Project Lazarus & Subject 23
            new[] {
                "[Radio crackle] ...urgent transmission, survivor.",
                "We've decoded more files from the facility. Project Lazarus was a military contract.",
                "They were trying to create... soldiers that couldn't die. Regeneration at the cellular level.",
                "Subject 23 was their breakthrough. Perfect regeneration. Perfect soldier.",
                "Then Subject 23 escaped containment three weeks ago. Patient zero.",
                "The mutation is accelerating. We're seeing new infected types.",
                "Exploders. Their bodies are... unstable. Keep your distance when they fall.",
                "The assault rifle should help with the larger hordes. It's in the shop.",
                "Three nights, survivor. The helicopter is on schedule.",
                "Whatever you do, don't let them surround you. EVAC Command out."
            },
            // Night 4 - Military Cover-up & Tank Infected
            new[] {
                "[Static] ...survivor, this is EVAC Command. Priority alert.",
                "We've intercepted military comms. Operation Deadlight was initiated.",
                "They're trying to cover this up. Destroy all evidence of Project Lazarus.",
                "Dr. Chen's last transmission mentioned Subject 23 was 'learning'. Adapting.",
                "The infected are testing our perimeters now. Probing for weaknesses.",
                "New threat detected: Tank-class infected. Heavily mutated. Armored tissue.",
                "They were soldiers once. Project Lazarus subjects that didn't die.",
                "Use incendiary rounds if you have them. Fire disrupts their regeneration.",
                "Two more nights. The extraction window is narrow.",
                "Whatever happens, the world needs to know the truth. EVAC Command out."
            },
            // Night 5 - Final Night: Subject 23 Boss Fight
            new[] {
                "[Radio] Survivor. This is it. Final night.",
                "Massive biological signature detected approaching your position.",
                "It's Subject 23. The original. The source of all of this.",
                "Dr. Chen's notes say it absorbed every sample, every test subject.",
                "It's been hunting survivors. Growing stronger with each one.",
                "Every infected you've killed... it felt. It's angry. And it's coming for you.",
                "This creature ended the world. Tonight, you end it.",
                "Helicopter is inbound. Hold until dawn. Do not let it reach the extraction point.",
                "If Subject 23 escapes the quarantine zone... there will be no stopping it.",
                "Everything we've done comes down to this moment.",
                "For everyone we've lost. For everyone who's still fighting.",
                "Make it count, survivor. EVAC Command... [voice breaks] ...we believe in you."
            }
        };

        private static readonly string[] dayTips = {
            "TIP: Explore the area during daylight. The infected are slower but still dangerous.",
            "TIP: Use buildings and obstacles as cover. Funnel enemies into kill zones.",
            "TIP: SHIFT to sprint. Essential for escaping Runner infected.",
            "TIP: Reload with R before night falls. An empty gun is a death sentence.",
            "TIP: The shop opens at dawn. Spend points on weapons and supplies.",
            "TIP: Watch your health. Medkits are rare but essential.",
            "TIP: Exploders can damage other infected. Use them strategically.",
            "TIP: Tank infected are slow but devastating. Kite them, don't tank them.",
            "TIP: Some infected glow red at night. They're more aggressive.",
            "TIP: The infected are drawn to noise. Sometimes silence is survival."
        };

        private static readonly string[] loreMessages = {
            "[Intercepted] Dr. Chen's Log: 'Subject 23 shows unprecedented regeneration. Wounds heal in seconds.'",
            "[Intercepted] Military Order: 'Project Lazarus is CODE BLACK. All evidence to be destroyed.'",
            "[Intercepted] Survivor Recording: 'They're not mindless. I saw them... communicating.'",
            "[Intercepted] Dr. Chen's Final Entry: 'I'm sorry. I thought I was saving lives.'",
            "[Intercepted] Unknown: 'Subject 23 escaped. It took the others with it. God help us all.'"
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
                "THE SUN IS SETTING. PREPARE YOURSELF.",
                "DARKNESS APPROACHES. THE INFECTED STIR.",
                "NIGHT FALLS. THEY'RE COMING.",
                "THE HORDE AWAKENS. STAND YOUR GROUND.",
                "FINAL NIGHT. SUBJECT 23 IS NEAR."
            };

            int index = Mathf.Clamp(night - 1, 0, warnings.Length - 1);
            StartCoroutine(ShowWarningTransmission(warnings[index]));
        }

        private IEnumerator ShowWarningTransmission(string text)
        {
            if (transmissionPanel == null || transmissionText == null) yield break;

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

        public void ShowSubject23Warning()
        {
            StartCoroutine(ShowBossWarning());
        }

        private IEnumerator ShowBossWarning()
        {
            if (transmissionPanel == null || transmissionText == null) yield break;

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
