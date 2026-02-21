using UnityEngine;
using System.Collections.Generic;

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

        [Header("Ambient")]
        [SerializeField] private AudioClip dayAmbient;
        [SerializeField] private AudioClip nightAmbient;

        [Header("Settings")]
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float ambientVolume = 0.3f;
        [SerializeField] private float crossfadeDuration = 2f;

        private Dictionary<string, AudioClip> sfxLibrary = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupAudioSources();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            var dayNightCycle = FindObjectOfType<DayNightCycle>();
            if (dayNightCycle != null)
            {
                dayNightCycle.OnDayStart += PlayDayAudio;
                dayNightCycle.OnNightStart += PlayNightAudio;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void SetupAudioSources()
        {
            if (musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                var sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (ambientSource == null)
            {
                var ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }

            musicSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
            ambientSource.volume = ambientVolume;
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    PlayMusic(menuMusic);
                    StopAmbient();
                    break;
                case GameState.DayPhase:
                    PlayDayAudio();
                    break;
                case GameState.NightPhase:
                    PlayNightAudio();
                    break;
                case GameState.GameOver:
                case GameState.Victory:
                    FadeOutMusic();
                    break;
            }
        }

        private void PlayDayAudio()
        {
            PlayMusic(dayMusic);
            PlayAmbient(dayAmbient);
        }

        private void PlayNightAudio()
        {
            PlayMusic(nightMusic);
            PlayAmbient(nightAmbient);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null) return;

            if (musicSource.clip == clip && musicSource.isPlaying) return;

            StartCoroutine(CrossfadeMusic(clip));
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float startVolume = musicSource.volume;

            while (musicSource.volume > 0)
            {
                musicSource.volume -= startVolume * Time.deltaTime / (crossfadeDuration / 2);
                yield return null;
            }

            musicSource.clip = newClip;
            musicSource.Play();

            while (musicSource.volume < musicVolume)
            {
                musicSource.volume += musicVolume * Time.deltaTime / (crossfadeDuration / 2);
                yield return null;
            }

            musicSource.volume = musicVolume;
        }

        public void FadeOutMusic()
        {
            StartCoroutine(FadeOutMusicCoroutine());
        }

        private System.Collections.IEnumerator FadeOutMusicCoroutine()
        {
            while (musicSource.volume > 0)
            {
                musicSource.volume -= musicVolume * Time.deltaTime / crossfadeDuration;
                yield return null;
            }

            musicSource.Stop();
        }

        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null || ambientSource == null) return;

            ambientSource.clip = clip;
            ambientSource.volume = ambientVolume;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null) return;

            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
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
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * volumeScale);
        }

        public void RegisterSFX(string name, AudioClip clip)
        {
            sfxLibrary[name] = clip;
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            if (ambientSource != null)
                ambientSource.volume = ambientVolume;
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
