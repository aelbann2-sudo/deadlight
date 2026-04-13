using UnityEngine;

namespace Deadlight.Audio
{
    public class ZombieSounds : MonoBehaviour
    {
        private AudioSource audioSource;
        private float idleSoundTimer;
        private float idleSoundInterval;
        private bool isAggressive;
        private bool isDead;
        private static bool audioInitialized;
        private static AudioClip[] groanClips;
        private static AudioClip[] growlClips;
        private static AudioClip hitReactClip;
        private static AudioClip deathClip;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 0.8f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 15f;
            audioSource.volume = 0.4f;

            InitializeClips();
        }

        private static void InitializeClips()
        {
            if (audioInitialized) return;

            groanClips = new AudioClip[4];
            for (int i = 0; i < 4; i++)
            {
                try { groanClips[i] = Deadlight.Audio.ProceduralAudioGenerator.GenerateZombieGroan(i); }
                catch { groanClips[i] = null; }
            }

            growlClips = new AudioClip[3];
            for (int i = 0; i < 3; i++)
            {
                try { growlClips[i] = Deadlight.Audio.ProceduralAudioGenerator.GenerateZombieGrowl(i); }
                catch { growlClips[i] = null; }
            }

            try { hitReactClip = Deadlight.Audio.ProceduralAudioGenerator.GenerateZombieHitReact(); }
            catch { hitReactClip = null; }

            try { deathClip = Deadlight.Audio.ProceduralAudioGenerator.GenerateZombieDeath(); }
            catch { deathClip = null; }

            audioInitialized = true;
        }

        private void Start()
        {
            idleSoundInterval = Random.Range(3f, 8f);
            idleSoundTimer = Random.Range(0f, idleSoundInterval);
        }

        private void Update()
        {
            if (isDead) return;

            idleSoundTimer -= Time.deltaTime;

            if (idleSoundTimer <= 0f)
            {
                PlayIdle();

                idleSoundTimer = isAggressive
                    ? Random.Range(3f, 8f)
                    : Random.Range(5f, 15f);
            }
        }

        public void SetAggressive(bool aggressive)
        {
            bool wasAggressive = isAggressive;
            isAggressive = aggressive;

            if (aggressive && !wasAggressive && growlClips != null && growlClips.Length > 0)
            {
                AudioClip clip = growlClips[Random.Range(0, growlClips.Length)];
                PlayClip(clip);
            }
        }

        public void PlayHitReact()
        {
            PlayClip(hitReactClip);
        }

        public void PlayDeath()
        {
            isDead = true;
            PlayClip(deathClip);
        }

        public void PlayIdle()
        {
            if (isAggressive && growlClips != null && growlClips.Length > 0)
            {
                PlayClip(growlClips[Random.Range(0, growlClips.Length)]);
            }
            else if (groanClips != null && groanClips.Length > 0)
            {
                PlayClip(groanClips[Random.Range(0, groanClips.Length)]);
            }
        }

        private void PlayClip(AudioClip clip, float pitchVariation = 0.1f)
        {
            if (clip == null || audioSource == null) return;

            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.PlayOneShot(clip);
        }
    }
}
