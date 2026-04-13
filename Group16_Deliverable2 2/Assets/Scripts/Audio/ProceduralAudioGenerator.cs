using UnityEngine;
using System.Collections.Generic;

namespace Deadlight.Audio
{
    public static class ProceduralAudioGenerator
    {
        private const int SampleRate = 44100;
        private static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private static System.Random _rng = new System.Random(42);

        // --- Helper Methods ---

        private static float SineWave(float time, float freq)
        {
            return Mathf.Sin(2f * Mathf.PI * freq * time);
        }

        private static float WhiteNoise()
        {
            return (float)_rng.NextDouble() * 2f - 1f;
        }

        private static float Envelope(float t, float attack, float decay, float sustain, float release, float duration)
        {
            if (t < 0f) return 0f;
            if (t < attack)
                return t / attack;
            if (t < attack + decay)
                return 1f - (1f - sustain) * ((t - attack) / decay);
            if (t < duration - release)
                return sustain;
            if (t < duration)
                return sustain * (1f - (t - (duration - release)) / release);
            return 0f;
        }

        private static AudioClip CreateClip(string name, float[] samples, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        // --- Public Generation Methods ---

        public static AudioClip GenerateGunshot(string type)
        {
            string key = "gunshot_" + type;
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration;
            float freqLow, freqHigh;
            float noiseMix;

            switch (type)
            {
                case "shotgun":
                    duration = 0.3f;
                    freqLow = 300f;
                    freqHigh = 800f;
                    noiseMix = 0.7f;
                    break;
                case "rifle":
                    duration = 0.2f;
                    freqLow = 500f;
                    freqHigh = 1500f;
                    noiseMix = 0.5f;
                    break;
                case "smg":
                    duration = 0.1f;
                    freqLow = 800f;
                    freqHigh = 2000f;
                    noiseMix = 0.45f;
                    break;
                default: // pistol
                    duration = 0.15f;
                    freqLow = 800f;
                    freqHigh = 2000f;
                    noiseMix = 0.5f;
                    break;
            }

            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float normalized = t / duration;

                float env = Envelope(t, 0.002f, duration * 0.3f, 0.2f, duration * 0.5f, duration);

                float toneSum = 0f;
                toneSum += SineWave(t, freqLow) * 0.5f;
                toneSum += SineWave(t, freqHigh) * 0.3f;
                toneSum += SineWave(t, (freqLow + freqHigh) * 0.5f) * 0.2f;

                float noise = WhiteNoise();
                float mixed = toneSum * (1f - noiseMix) + noise * noiseMix;

                if (type == "rifle" && normalized > 0.5f)
                {
                    float echoT = t - duration * 0.5f;
                    float echoEnv = Mathf.Exp(-echoT * 10f) * 0.3f;
                    mixed += (SineWave(echoT, freqLow * 0.8f) * 0.5f + WhiteNoise() * 0.5f) * echoEnv;
                }

                if (type == "shotgun")
                {
                    mixed += WhiteNoise() * env * 0.3f;
                }

                samples[i] = Mathf.Clamp(mixed * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateReload()
        {
            string key = "reload";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 0.8f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float sample = 0f;

                // Click at 0.0s
                float clickT = t;
                if (clickT >= 0f && clickT < 0.05f)
                {
                    float clickEnv = Mathf.Exp(-clickT * 80f);
                    sample += (SineWave(clickT, 3000f) * 0.6f + WhiteNoise() * 0.4f) * clickEnv;
                }

                // Slide from 0.3s to 0.4s
                float slideT = t - 0.3f;
                if (slideT >= 0f && slideT < 0.1f)
                {
                    float slideProgress = slideT / 0.1f;
                    float slideFreq = Lerp(2000f, 800f, slideProgress);
                    float slideEnv = Envelope(slideT, 0.005f, 0.04f, 0.4f, 0.04f, 0.1f);
                    sample += (SineWave(slideT, slideFreq) * 0.5f + WhiteNoise() * 0.3f) * slideEnv;
                }

                // Click at 0.6s
                float click2T = t - 0.6f;
                if (click2T >= 0f && click2T < 0.05f)
                {
                    float click2Env = Mathf.Exp(-click2T * 80f);
                    sample += (SineWave(click2T, 3200f) * 0.6f + WhiteNoise() * 0.4f) * click2Env;
                }

                samples[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateEmptyClick()
        {
            string key = "empty_click";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 0.1f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 100f);
                samples[i] = Mathf.Clamp((SineWave(t, 4000f) * 0.5f + WhiteNoise() * 0.5f) * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateFootstep(int variant)
        {
            string key = "footstep_" + variant;
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            variant = Mathf.Clamp(variant, 0, 3);

            float duration = 0.15f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float baseFreq = 80f + variant * 35f;
            float noiseAmount = 0.15f + variant * 0.05f;
            float amplitude = 0.7f + variant * 0.07f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 25f);

                float tone = SineWave(t, baseFreq) * 0.6f + SineWave(t, baseFreq * 1.5f) * 0.2f;
                float noise = WhiteNoise() * noiseAmount;

                samples[i] = Mathf.Clamp((tone + noise) * env * amplitude, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateZombieGroan(int variant)
        {
            string key = "zombie_groan_" + variant;
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            variant = Mathf.Clamp(variant, 0, 3);

            float[] durations = { 1.5f, 2.0f, 2.2f, 2.5f };
            float[] baseFreqs = { 70f, 60f, 90f, 80f };
            float[] modRates = { 3f, 2f, 5f, 4f };

            float duration = durations[variant];
            float baseFreq = baseFreqs[variant];
            float modRate = modRates[variant];

            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;

                float env = Envelope(t, 0.2f, 0.3f, 0.6f, duration * 0.3f, duration);

                float freqWobble = baseFreq + SineWave(t, modRate * 0.5f) * 15f;
                float tone = SineWave(t, freqWobble) * 0.6f + SineWave(t, freqWobble * 1.5f) * 0.15f;

                float ampMod = 0.6f + 0.4f * SineWave(t, modRate);

                float breathNoise = WhiteNoise() * 0.15f * Clamp01(SineWave(t, modRate * 0.7f) * 0.5f + 0.5f);

                samples[i] = Mathf.Clamp((tone * ampMod + breathNoise) * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateZombieGrowl(int variant)
        {
            string key = "zombie_growl_" + variant;
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            variant = Mathf.Clamp(variant, 0, 2);

            float[] durations = { 0.8f, 1.0f, 1.2f };
            float[] baseFreqs = { 180f, 220f, 150f };
            float[] modRates = { 10f, 8f, 12f };

            float duration = durations[variant];
            float baseFreq = baseFreqs[variant];
            float modRate = modRates[variant];

            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;

                float env = Envelope(t, 0.05f, 0.15f, 0.7f, duration * 0.3f, duration);

                float freqWobble = baseFreq + SineWave(t, modRate * 0.5f) * 30f;
                float tone = SineWave(t, freqWobble) * 0.4f
                           + SineWave(t, freqWobble * 0.5f) * 0.2f
                           + SineWave(t, freqWobble * 2f) * 0.1f;

                float ampMod = 0.5f + 0.5f * SineWave(t, modRate);
                float noise = WhiteNoise() * 0.35f;

                samples[i] = Mathf.Clamp((tone * ampMod + noise) * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateZombieHitReact()
        {
            string key = "zombie_hit_react";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 0.3f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float normalized = t / duration;

                float env = Mathf.Exp(-t * 10f);
                float freq = Lerp(300f, 100f, normalized);
                float tone = SineWave(t, freq) * 0.5f;
                float noise = WhiteNoise() * 0.4f * Mathf.Exp(-t * 15f);

                samples[i] = Mathf.Clamp((tone + noise) * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateZombieDeath()
        {
            string key = "zombie_death";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 1.0f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float normalized = t / duration;

                float volumeEnv = 1f - normalized * 0.8f;
                float freq = Lerp(200f, 40f, normalized);
                float tone = SineWave(t, freq) * 0.5f + SineWave(t, freq * 0.5f) * 0.2f;

                float noiseAmount = normalized * 0.5f;
                float noise = WhiteNoise() * noiseAmount;

                samples[i] = Mathf.Clamp((tone + noise) * volumeEnv, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateExplosion()
        {
            string key = "explosion";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 0.5f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;

                float env = (t < 0.01f)
                    ? t / 0.01f
                    : Mathf.Exp(-(t - 0.01f) * 6f);

                float rumble = SineWave(t, 40f) * 0.3f
                             + SineWave(t, 70f) * 0.25f
                             + SineWave(t, 100f) * 0.15f;

                float noise = WhiteNoise() * 0.6f * Mathf.Exp(-t * 8f);

                samples[i] = Mathf.Clamp((rumble + noise) * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateHeartbeat()
        {
            string key = "heartbeat";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 0.8f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float sample = 0f;

                // First pulse at 0.0s
                if (t < 0.1f)
                {
                    float pulseEnv = Mathf.Exp(-t * 30f);
                    sample += SineWave(t, 40f) * pulseEnv * 0.8f;
                }

                // Second pulse at 0.3s
                float t2 = t - 0.3f;
                if (t2 >= 0f && t2 < 0.1f)
                {
                    float pulseEnv = Mathf.Exp(-t2 * 30f);
                    sample += SineWave(t2, 40f) * pulseEnv * 0.6f;
                }

                samples[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateAmbientWind()
        {
            string key = "ambient_wind";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 3.0f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float prevFiltered = 0f;
            float filterAlpha = 0.995f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;

                float raw = WhiteNoise();
                prevFiltered = filterAlpha * prevFiltered + (1f - filterAlpha) * raw;

                float volMod = 0.3f + 0.2f * SineWave(t, 0.4f) + 0.1f * SineWave(t, 0.15f);

                // Smooth loop boundaries
                float fadeLen = 0.1f;
                float loopFade = 1f;
                if (t < fadeLen)
                    loopFade = t / fadeLen;
                else if (t > duration - fadeLen)
                    loopFade = (duration - t) / fadeLen;

                samples[i] = Mathf.Clamp(prevFiltered * volMod * loopFade, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GeneratePickup()
        {
            string key = "pickup";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 0.3f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float normalized = t / duration;

                float env = Envelope(t, 0.01f, 0.05f, 0.5f, 0.15f, duration);

                float freq = Lerp(400f, 800f, normalized);
                float tone = SineWave(t, freq) * 0.5f
                           + SineWave(t, freq * 2f) * 0.25f
                           + SineWave(t, freq * 3f) * 0.1f;

                samples[i] = Mathf.Clamp(tone * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateRadioStatic()
        {
            string key = "radio_static";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 0.4f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float prevFiltered = 0f;
            float filterAlpha = 0.92f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Envelope(t, 0.01f, 0.08f, 0.6f, 0.2f, duration);

                float raw = WhiteNoise();
                prevFiltered = filterAlpha * prevFiltered + (1f - filterAlpha) * raw;

                float crackle = SineWave(t, 120f) * 0.15f;
                float hiss = prevFiltered * 0.7f + crackle;

                samples[i] = Mathf.Clamp(hiss * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateAlarmSiren()
        {
            string key = "alarm_siren";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 2.0f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float normalized = t / duration;

                float env = Envelope(t, 0.1f, 0.2f, 0.7f, 0.5f, duration);
                float sweep = Lerp(400f, 900f, (SineWave(t, 1.5f) + 1f) * 0.5f);

                float tone = SineWave(t, sweep) * 0.5f + SineWave(t, sweep * 2f) * 0.15f;

                samples[i] = Mathf.Clamp(tone * env, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateHelicopterApproach()
        {
            string key = "helicopter_approach";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 4.0f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float prevFiltered = 0f;
            float filterAlpha = 0.98f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float normalized = t / duration;

                float volumeRamp = normalized * normalized;

                float bladeFreq = 22f;
                float bladePulse = (SineWave(t, bladeFreq) + 1f) * 0.5f;
                bladePulse = bladePulse * 0.6f + 0.4f;

                float engineTone = SineWave(t, 85f) * 0.3f + SineWave(t, 170f) * 0.15f;

                float raw = WhiteNoise();
                prevFiltered = filterAlpha * prevFiltered + (1f - filterAlpha) * raw;
                float windNoise = prevFiltered * 0.4f;

                float mixed = (engineTone + windNoise) * bladePulse * volumeRamp;

                samples[i] = Mathf.Clamp(mixed, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateDayMusic()
        {
            string key = "music_day";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 16f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float[] notes = { 261.6f, 293.7f, 329.6f, 349.2f, 392.0f, 440.0f };
            float noteLen = 2f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIdx = (int)(t / noteLen) % notes.Length;
                float freq = notes[noteIdx];

                float pad = SineWave(t, freq) * 0.12f +
                            SineWave(t, freq * 1.5f) * 0.06f +
                            SineWave(t, freq * 0.5f) * 0.08f;

                float lfo = (SineWave(t, 0.3f) + 1f) * 0.5f;
                float env = 0.15f + lfo * 0.1f;

                samples[i] = Mathf.Clamp(pad * env * 3f, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateNightMusic()
        {
            string key = "music_night";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 16f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float prevFiltered = 0f;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;

                float drone = SineWave(t, 55f) * 0.18f +
                              SineWave(t, 82.4f) * 0.08f;

                float pulse = (SineWave(t, 1.2f) + 1f) * 0.5f;

                float raw = WhiteNoise();
                prevFiltered = 0.995f * prevFiltered + 0.005f * raw;
                float rumble = prevFiltered * 0.12f;

                float heartbeat = 0f;
                float beatPhase = t % 1.2f;
                if (beatPhase < 0.08f)
                    heartbeat = SineWave(beatPhase, 45f) * Envelope(beatPhase, 0.01f, 0.02f, 0.5f, 0.04f, 0.08f) * 0.2f;
                else if (beatPhase > 0.15f && beatPhase < 0.22f)
                    heartbeat = SineWave(beatPhase - 0.15f, 50f) * Envelope(beatPhase - 0.15f, 0.01f, 0.02f, 0.4f, 0.03f, 0.07f) * 0.15f;

                samples[i] = Mathf.Clamp((drone * pulse + rumble + heartbeat), -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateBossMusic()
        {
            string key = "music_boss";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 12f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;

                float bass = SineWave(t, 41.2f) * 0.25f + SineWave(t, 82.4f) * 0.12f;

                float beatPos = t % 0.5f;
                float kick = beatPos < 0.05f ? SineWave(beatPos, Lerp(120f, 40f, beatPos / 0.05f)) * (1f - beatPos / 0.05f) * 0.3f : 0f;

                float tension = SineWave(t, 110f) * 0.06f +
                                SineWave(t, 116.5f) * 0.06f;

                float riser = SineWave(t, Lerp(200f, 800f, (t % 6f) / 6f)) * 0.04f;

                samples[i] = Mathf.Clamp(bass + kick + tension + riser, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }

        public static AudioClip GenerateDawnMusic()
        {
            string key = "music_dawn";
            if (_cache.TryGetValue(key, out AudioClip cached)) return cached;

            float duration = 10f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float norm = t / duration;

                float chord = SineWave(t, 261.6f) * 0.1f +
                              SineWave(t, 329.6f) * 0.08f +
                              SineWave(t, 392.0f) * 0.06f +
                              SineWave(t, 523.3f) * 0.04f;

                float swell = norm * norm;
                float shimmer = SineWave(t, 1760f) * 0.015f * (SineWave(t, 3f) + 1f) * 0.5f;

                samples[i] = Mathf.Clamp((chord + shimmer) * swell * 2f, -1f, 1f);
            }

            AudioClip clip = CreateClip(key, samples, SampleRate);
            _cache[key] = clip;
            return clip;
        }
    }
}
