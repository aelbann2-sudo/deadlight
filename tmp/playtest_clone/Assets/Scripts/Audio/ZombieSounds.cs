using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Audio
{
    public class ZombieSounds : MonoBehaviour
    {
        private const float GlobalIdleVocalCooldown = 0.08f;
        private const float GlobalCombatVocalCooldown = 0.03f;

        private AudioSource audioSource;
        private float idleSoundTimer;
        private float idleSoundInterval;
        private float distanceSampleTimer;
        private float distanceToPlayer = 999f;
        private bool isAggressive;
        private bool isDead;
        private Transform playerTransform;
        private static bool audioInitialized;
        private static float lastGlobalVocalTime;
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
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 18f;
            audioSource.spread = 20f;
            audioSource.dopplerLevel = 0f;
            audioSource.volume = 0.38f;

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
            distanceSampleTimer = Random.Range(0f, 0.35f);
            RefreshPlayerDistance();
        }

        private void Update()
        {
            if (isDead) return;

            distanceSampleTimer -= Time.deltaTime;
            if (distanceSampleTimer <= 0f)
            {
                RefreshPlayerDistance();
                distanceSampleTimer = Random.Range(0.2f, 0.45f);
            }

            idleSoundTimer -= Time.deltaTime;

            if (idleSoundTimer <= 0f)
            {
                if (ShouldPlayIdleAtCurrentDistance())
                {
                    PlayIdle();
                }

                idleSoundTimer = GetNextIdleInterval();
            }
        }

        public void SetAggressive(bool aggressive)
        {
            bool wasAggressive = isAggressive;
            isAggressive = aggressive;

            if (aggressive && !wasAggressive && growlClips != null && growlClips.Length > 0)
            {
                AudioClip clip = growlClips[Random.Range(0, growlClips.Length)];
                PlayClip(clip, pitchVariation: 0.08f, volumeMultiplier: 1.1f);
                AudioManager.Instance?.SignalCombatPeak(0.1f, 0.5f);
            }
        }

        public void PlayHitReact()
        {
            PlayClip(hitReactClip, pitchVariation: 0.1f, volumeMultiplier: 1f, bypassGlobalCooldown: true);
            AudioManager.Instance?.SignalCombatPeak(0.07f, 0.4f);
        }

        public void PlayDeath()
        {
            isDead = true;
            PlayClip(deathClip, pitchVariation: 0.06f, volumeMultiplier: 1.2f, bypassGlobalCooldown: true);
            AudioManager.Instance?.SignalCombatPeak(0.12f, 0.7f);
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

        private void PlayClip(
            AudioClip clip,
            float pitchVariation = 0.1f,
            float volumeMultiplier = 1f,
            bool bypassGlobalCooldown = false)
        {
            if (clip == null || audioSource == null) return;

            float cooldown = isAggressive ? GlobalCombatVocalCooldown : GlobalIdleVocalCooldown;
            if (!bypassGlobalCooldown && Time.time - lastGlobalVocalTime < cooldown)
            {
                return;
            }

            if (!bypassGlobalCooldown && audioSource.isPlaying && Random.value < 0.45f)
            {
                return;
            }

            float distanceVolume = 1f;
            if (playerTransform != null)
            {
                float normalized = Mathf.InverseLerp(2f, 20f, distanceToPlayer);
                distanceVolume = Mathf.Lerp(1f, 0.35f, normalized);
            }

            float aggressionBoost = isAggressive ? 1.15f : 1f;
            float finalVolume = Mathf.Clamp01(0.38f * volumeMultiplier * distanceVolume * aggressionBoost);

            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.PlayOneShot(clip, finalVolume);
            lastGlobalVocalTime = Time.time;
        }

        private float GetNextIdleInterval()
        {
            float minInterval = isAggressive ? 2.8f : 4.8f;
            float maxInterval = isAggressive ? 6.8f : 12.5f;

            if (distanceToPlayer > 14f)
            {
                minInterval *= 1.25f;
                maxInterval *= 1.25f;
            }

            return Random.Range(minInterval, maxInterval);
        }

        private bool ShouldPlayIdleAtCurrentDistance()
        {
            if (playerTransform == null)
            {
                return true;
            }

            if (distanceToPlayer > 20f)
            {
                return Random.value < 0.25f;
            }

            if (distanceToPlayer > 14f)
            {
                return Random.value < 0.6f;
            }

            return true;
        }

        private void RefreshPlayerDistance()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }

            if (playerTransform == null)
            {
                distanceToPlayer = 999f;
                return;
            }

            distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        }
    }
}
