using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deadlight.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip dayMusic;
        [SerializeField] private AudioClip nightMusic;
        [SerializeField] private AudioClip bossMusic;
        [SerializeField] private AudioClip dawnMusic;

        [Header("Ambient")]
        [SerializeField] private AudioClip dayAmbient;
        [SerializeField] private AudioClip nightAmbient;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.82f;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.36f;
        [SerializeField, Min(0.05f)] private float crossfadeDuration = 3.2f;

        [Header("Dynamic Mix")]
        [SerializeField, Range(0f, 1f)] private float dayBaseTension = 0.05f;
        [SerializeField, Range(0f, 1f)] private float nightBaseTension = 0.28f;
        [SerializeField, Range(0f, 1f)] private float bossBaseTension = 0.8f;
        [SerializeField, Min(0.1f)] private float tensionLerpSpeed = 1.15f;
        [SerializeField, Range(1f, 1.35f)] private float maxTensionMusicMultiplier = 1.06f;
        [SerializeField, Range(0.4f, 1f)] private float maxTensionAmbientMultiplier = 0.84f;
        [SerializeField, Range(0.9f, 1.15f)] private float maxTensionMusicPitch = 1.015f;

        [Header("Voice Ducking")]
        [SerializeField, Range(0f, 1f)] private float defaultVoiceDuckAmount = 0.24f;
        [SerializeField, Min(0.1f)] private float duckLerpSpeed = 3.2f;

        [Header("3D SFX Pool")]
        [SerializeField, Range(4, 24)] private int positionalSourcePoolSize = 10;
        [SerializeField, Range(5f, 50f)] private float positionalMaxDistance = 20f;

        private readonly Dictionary<string, AudioClip> sfxLibrary = new Dictionary<string, AudioClip>();
        private readonly List<AudioSource> positionalSources = new List<AudioSource>();

        private Coroutine musicTransitionCoroutine;
        private Coroutine ambientTransitionCoroutine;
        private Coroutine fadeOutCoroutine;
        private DayNightCycle dayNightCycle;

        private float baseTension;
        private float transientTension;
        private float transientTensionTimer;
        private float currentTension;

        private float duckTimer;
        private float duckTargetAmount;
        private float currentDuckAmount;
        private float currentSfxMixMultiplier = 1f;

        private float musicBlend = 1f;
        private float ambientBlend = 1f;
        private int positionalCursor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
        }

        private void Start()
        {
            GenerateProceduralAudio();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }

            dayNightCycle = FindFirstObjectByType<DayNightCycle>();
            if (dayNightCycle != null)
            {
                dayNightCycle.OnDayStart += PlayDayAudio;
                dayNightCycle.OnNightStart += PlayNightAudio;
            }
        }

        private void Update()
        {
            UpdateDynamicMix(Time.unscaledDeltaTime);
        }

        private void GenerateProceduralAudio()
        {
            try
            {
                if (nightAmbient == null)
                {
                    nightAmbient = Audio.ProceduralAudioGenerator.GenerateAmbientWind();
                }

                if (dayAmbient == null)
                {
                    dayAmbient = Audio.ProceduralAudioGenerator.GenerateAmbientWind();
                }

                if (dayMusic == null)
                {
                    dayMusic = Audio.ProceduralAudioGenerator.GenerateDayMusic();
                }

                if (nightMusic == null)
                {
                    nightMusic = Audio.ProceduralAudioGenerator.GenerateNightMusic();
                }

                if (bossMusic == null)
                {
                    bossMusic = Audio.ProceduralAudioGenerator.GenerateBossMusic();
                }

                if (dawnMusic == null)
                {
                    dawnMusic = Audio.ProceduralAudioGenerator.GenerateDawnMusic();
                }

                RegisterSFX("gunshot_pistol", Audio.ProceduralAudioGenerator.GenerateGunshot("pistol"));
                RegisterSFX("gunshot_shotgun", Audio.ProceduralAudioGenerator.GenerateGunshot("shotgun"));
                RegisterSFX("gunshot_smg", Audio.ProceduralAudioGenerator.GenerateGunshot("smg"));
                RegisterSFX("reload", Audio.ProceduralAudioGenerator.GenerateReload());
                RegisterSFX("explosion", Audio.ProceduralAudioGenerator.GenerateExplosion());
                RegisterSFX("pickup", Audio.ProceduralAudioGenerator.GeneratePickup());
                RegisterSFX("radio_static", Audio.ProceduralAudioGenerator.GenerateRadioStatic());
                RegisterSFX("alarm_siren", Audio.ProceduralAudioGenerator.GenerateAlarmSiren());
                RegisterSFX("heartbeat", Audio.ProceduralAudioGenerator.GenerateHeartbeat());
                RegisterSFX("helicopter_approach", Audio.ProceduralAudioGenerator.GenerateHelicopterApproach());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioManager] Failed to generate procedural audio: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }

            if (dayNightCycle != null)
            {
                dayNightCycle.OnDayStart -= PlayDayAudio;
                dayNightCycle.OnNightStart -= PlayNightAudio;
            }
        }

        private void SetupAudioSources()
        {
            if (musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
            }

            if (sfxSource == null)
            {
                var sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
            }

            if (ambientSource == null)
            {
                var ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
            }

            Configure2DSource(musicSource, true);
            Configure2DSource(sfxSource, false);
            Configure2DSource(ambientSource, true);

            musicSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
            ambientSource.volume = ambientVolume;

            BuildPositionalSfxPool();
        }

        private static void Configure2DSource(AudioSource source, bool loop)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        private void BuildPositionalSfxPool()
        {
            positionalSources.Clear();

            for (int i = 0; i < positionalSourcePoolSize; i++)
            {
                var sourceObj = new GameObject($"PositionalSFX_{i}");
                sourceObj.transform.SetParent(transform);

                var source = sourceObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 1f;
                source.maxDistance = positionalMaxDistance;
                source.dopplerLevel = 0f;

                positionalSources.Add(source);
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    SetBaseTension(0f);
                    PlayMusic(menuMusic);
                    StopAmbient();
                    break;
                case GameState.DayPhase:
                    PlayDayAudio();
                    break;
                case GameState.NightPhase:
                    PlayNightAudio();
                    break;
                case GameState.DawnPhase:
                    SetBaseTension(0.16f);
                    PlayMusic(dawnMusic);
                    StopAmbient();
                    break;
                case GameState.GameOver:
                case GameState.Victory:
                    SetBaseTension(0f);
                    FadeOutMusic();
                    break;
            }
        }

        public void PlayBossMusic()
        {
            SetBaseTension(bossBaseTension);
            PlayMusic(bossMusic);
        }

        private void PlayDayAudio()
        {
            SetBaseTension(dayBaseTension);
            PlayMusic(dayMusic);
            PlayAmbient(dayAmbient);
        }

        private void PlayNightAudio()
        {
            float levelBonus = 0f;
            if (GameManager.Instance != null)
            {
                levelBonus = Mathf.InverseLerp(1f, GameManager.TotalLevels, GameManager.Instance.CurrentLevel) * 0.12f;
            }

            SetBaseTension(Mathf.Clamp01(nightBaseTension + levelBonus));
            PlayMusic(nightMusic);
            PlayAmbient(nightAmbient);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null)
            {
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            if (fadeOutCoroutine != null)
            {
                StopCoroutine(fadeOutCoroutine);
                fadeOutCoroutine = null;
            }

            if (musicTransitionCoroutine != null)
            {
                StopCoroutine(musicTransitionCoroutine);
            }

            if (!musicSource.isPlaying || musicSource.clip == null)
            {
                musicSource.clip = clip;
                musicBlend = 1f;
                musicSource.Play();
                return;
            }

            musicTransitionCoroutine = StartCoroutine(CrossfadeMusic(clip));
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float halfDuration = Mathf.Max(0.05f, crossfadeDuration * 0.5f);

            for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
            {
                musicBlend = Mathf.Lerp(1f, 0f, t / halfDuration);
                yield return null;
            }

            musicBlend = 0f;
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.Play();

            for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
            {
                musicBlend = Mathf.Lerp(0f, 1f, t / halfDuration);
                yield return null;
            }

            musicBlend = 1f;
            musicTransitionCoroutine = null;
        }

        public void FadeOutMusic()
        {
            if (musicSource == null)
            {
                return;
            }

            if (musicTransitionCoroutine != null)
            {
                StopCoroutine(musicTransitionCoroutine);
                musicTransitionCoroutine = null;
            }

            if (fadeOutCoroutine != null)
            {
                StopCoroutine(fadeOutCoroutine);
            }

            fadeOutCoroutine = StartCoroutine(FadeOutMusicCoroutine());
        }

        private IEnumerator FadeOutMusicCoroutine()
        {
            float startBlend = musicBlend;
            float duration = Mathf.Max(0.05f, crossfadeDuration);

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                musicBlend = Mathf.Lerp(startBlend, 0f, t / duration);
                yield return null;
            }

            musicBlend = 0f;
            musicSource.Stop();
            fadeOutCoroutine = null;
        }

        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null || ambientSource == null)
            {
                return;
            }

            if (ambientSource.clip == clip && ambientSource.isPlaying)
            {
                return;
            }

            if (ambientTransitionCoroutine != null)
            {
                StopCoroutine(ambientTransitionCoroutine);
            }

            if (!ambientSource.isPlaying || ambientSource.clip == null)
            {
                ambientSource.clip = clip;
                ambientBlend = 1f;
                ambientSource.Play();
                return;
            }

            ambientTransitionCoroutine = StartCoroutine(CrossfadeAmbient(clip));
        }

        private IEnumerator CrossfadeAmbient(AudioClip newClip)
        {
            float halfDuration = Mathf.Max(0.05f, crossfadeDuration * 0.45f);

            for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
            {
                ambientBlend = Mathf.Lerp(1f, 0f, t / halfDuration);
                yield return null;
            }

            ambientBlend = 0f;
            ambientSource.Stop();
            ambientSource.clip = newClip;
            ambientSource.Play();

            for (float t = 0f; t < halfDuration; t += Time.unscaledDeltaTime)
            {
                ambientBlend = Mathf.Lerp(0f, 1f, t / halfDuration);
                yield return null;
            }

            ambientBlend = 1f;
            ambientTransitionCoroutine = null;
        }

        public void StopAmbient()
        {
            if (ambientSource == null)
            {
                return;
            }

            if (ambientTransitionCoroutine != null)
            {
                StopCoroutine(ambientTransitionCoroutine);
                ambientTransitionCoroutine = null;
            }

            ambientSource.Stop();
            ambientBlend = 0f;
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, GetEffectiveSfxGain(volumeScale));
        }

        public void PlaySFX(string clipName, float volumeScale = 1f)
        {
            if (sfxLibrary.TryGetValue(clipName, out AudioClip clip))
            {
                PlaySFX(clip, volumeScale);
            }
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            PlaySFXAtPosition(clip, position, volumeScale, 1f, 0f);
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale, float pitch, float randomPitchRange)
        {
            if (clip == null)
            {
                return;
            }

            var source = GetAvailablePositionalSource();
            if (source == null)
            {
                AudioSource.PlayClipAtPoint(clip, position, GetEffectiveSfxGain(volumeScale));
                return;
            }

            source.transform.position = position;
            source.pitch = Mathf.Clamp(pitch + Random.Range(-randomPitchRange, randomPitchRange), 0.7f, 1.4f);
            source.panStereo = Random.Range(-0.05f, 0.05f);
            source.PlayOneShot(clip, GetEffectiveSfxGain(volumeScale));
        }

        private AudioSource GetAvailablePositionalSource()
        {
            if (positionalSources.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < positionalSources.Count; i++)
            {
                if (!positionalSources[i].isPlaying)
                {
                    return positionalSources[i];
                }
            }

            positionalCursor = (positionalCursor + 1) % positionalSources.Count;
            return positionalSources[positionalCursor];
        }

        public void RegisterSFX(string name, AudioClip clip)
        {
            if (string.IsNullOrWhiteSpace(name) || clip == null)
            {
                return;
            }

            sfxLibrary[name] = clip;
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
        }

        public void SetBaseTension(float normalized)
        {
            baseTension = Mathf.Clamp01(normalized);
        }

        public void SignalCombatPeak(float amount = 0.15f, float holdSeconds = 0.8f)
        {
            if (amount <= 0f)
            {
                return;
            }

            transientTension = Mathf.Clamp01(transientTension + (amount * 0.6f));
            transientTensionTimer = Mathf.Max(transientTensionTimer, Mathf.Max(0f, holdSeconds * 0.85f));
        }

        public void DuckForVoice(float duration, float amount = -1f)
        {
            if (duration <= 0f)
            {
                return;
            }

            float targetAmount = amount < 0f ? defaultVoiceDuckAmount : amount;
            duckTargetAmount = Mathf.Clamp01(Mathf.Max(duckTargetAmount, targetAmount));
            duckTimer = Mathf.Max(duckTimer, duration);
        }

        private void UpdateDynamicMix(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (transientTensionTimer > 0f)
            {
                transientTensionTimer -= deltaTime;
            }
            else
            {
                transientTension = Mathf.MoveTowards(transientTension, 0f, deltaTime * 0.22f);
            }

            float targetTension = Mathf.Clamp01(baseTension + transientTension);
            currentTension = Mathf.MoveTowards(currentTension, targetTension, deltaTime * tensionLerpSpeed);

            if (duckTimer > 0f)
            {
                duckTimer -= deltaTime;
            }
            else
            {
                duckTargetAmount = 0f;
            }

            currentDuckAmount = Mathf.MoveTowards(currentDuckAmount, duckTargetAmount, deltaTime * duckLerpSpeed);

            float musicTensionMultiplier = Mathf.Lerp(1f, maxTensionMusicMultiplier, currentTension);
            float ambientTensionMultiplier = Mathf.Lerp(1f, maxTensionAmbientMultiplier, currentTension);
            float musicDuckMultiplier = 1f - currentDuckAmount;
            float ambientDuckMultiplier = 1f - (currentDuckAmount * 0.6f);
            float sfxDuckMultiplier = 1f - (currentDuckAmount * 0.15f);

            float targetMusicVolume = Mathf.Clamp01(musicVolume * musicBlend * musicTensionMultiplier * musicDuckMultiplier);
            float targetAmbientVolume = Mathf.Clamp01(ambientVolume * ambientBlend * ambientTensionMultiplier * ambientDuckMultiplier);
            currentSfxMixMultiplier = Mathf.MoveTowards(currentSfxMixMultiplier, sfxDuckMultiplier, deltaTime * 4.2f);

            if (musicSource != null)
            {
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetMusicVolume, deltaTime * 3f);
                musicSource.pitch = Mathf.Lerp(1f, maxTensionMusicPitch, currentTension);
            }

            if (ambientSource != null)
            {
                ambientSource.volume = Mathf.MoveTowards(ambientSource.volume, targetAmbientVolume, deltaTime * 3f);
            }

            if (sfxSource != null)
            {
                sfxSource.volume = 1f;
            }
        }

        private float GetEffectiveSfxGain(float volumeScale)
        {
            return Mathf.Max(0f, sfxVolume * currentSfxMixMultiplier * volumeScale);
        }

        public void PauseAll()
        {
            musicSource?.Pause();
            ambientSource?.Pause();
        }

        public void ResumeAll()
        {
            musicSource?.UnPause();
            ambientSource?.UnPause();
        }
    }
}
