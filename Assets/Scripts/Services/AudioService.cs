using UnityEngine;

namespace BatsSolitaire.Services
{
    public sealed class AudioService : MonoBehaviour
    {
        private AudioSource effects;
        private AudioSource music;
        private AudioClip move;
        private AudioClip invalid;
        private AudioClip win;
        private PlayerSettings settings;

        public void Initialize(PlayerSettings playerSettings)
        {
            settings = playerSettings;
            effects = gameObject.AddComponent<AudioSource>();
            music = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            music.loop = true;
            move = CardSlide();
            invalid = Tone("Invalid", 170f, 0.09f, 0.1f);
            win = Arpeggio();
            music.clip = Ambient();
            ApplyVolumes();
            music.Play();
        }

        public void ApplyVolumes()
        {
            if (settings == null) return;
            effects.volume = settings.EffectsVolume;
            music.volume = settings.MusicVolume * 0.25f;
        }

        public void PlayMove() => effects.PlayOneShot(move);
        public void PlayInvalid() => effects.PlayOneShot(invalid);
        public void PlayWin()
        {
            effects.PlayOneShot(win);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (settings.Haptics) Handheld.Vibrate();
#endif
        }

        private static AudioClip Tone(string name, float frequency, float duration, float amplitude)
        {
            const int sampleRate = 44100;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var envelope = Mathf.Sin(Mathf.PI * i / (samples - 1f));
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * amplitude * envelope;
            }
            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Arpeggio()
        {
            const int sampleRate = 44100;
            const float duration = 0.8f;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            var notes = new[] { 261.63f, 329.63f, 392f, 523.25f };
            for (var i = 0; i < samples; i++)
            {
                var note = notes[Mathf.Min(notes.Length - 1, (int)(i / (samples / (float)notes.Length)))];
                var noteSamples = samples / notes.Length;
                var phase = (i % noteSamples) / (float)noteSamples;
                var envelope = Mathf.Sin(Mathf.PI * phase) * (1f - i / (float)samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * note * (i % noteSamples) / sampleRate) * 0.16f * envelope;
            }
            var clip = AudioClip.Create("Win", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Ambient()
        {
            const int sampleRate = 22050;
            const int seconds = 8;
            var data = new float[sampleRate * seconds];
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)sampleRate;
                // Integer cycles across the loop avoid a discontinuity at the seam.
                data[i] = (Mathf.Sin(2f * Mathf.PI * 110f * t) + Mathf.Sin(2f * Mathf.PI * 165f * t)) * 0.025f;
            }
            var clip = AudioClip.Create("Ambient", data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CardSlide()
        {
            const int rate = 44100;
            var data = new float[5292];
            var random = new System.Random(42);
            var filtered = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.35f);
                data[i] = filtered * Mathf.Sin(Mathf.PI * i / (data.Length - 1f)) * 0.19f;
            }
            var clip = AudioClip.Create("CardSlide", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
