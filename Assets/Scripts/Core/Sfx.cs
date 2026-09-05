using System.Collections.Generic;
using UnityEngine;

namespace BomberGhst
{
    public enum Sound { Place, Explode, Pickup, Death, Kick, Menu, Tick, Fanfare, Drop }

    /// Chiptune-ish sound effects and BGM, synthesised at startup so the
    /// project needs no audio files.
    public class Sfx : MonoBehaviour
    {
        const int Rate = 22050;

        public static Sfx I { get; private set; }

        readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
        AudioSource sfxSource;
        AudioSource musicSource;

        /// Lives at the scene root and survives scene loads, so the front end
        /// and the match share one synthesised sound bank.
        public static Sfx Ensure()
        {
            if (I != null) return I;
            var go = new GameObject("Audio");
            DontDestroyOnLoad(go);
            var s = go.AddComponent<Sfx>();
            s.Build();
            I = s;
            return s;
        }

        void Build()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.5f;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.22f;

            clips[Sound.Place] = Render("place", 0.12f, (t, n) =>
                Square(660f - 320f * n, t) * Env(n, 0.01f, 0.11f) * 0.5f);

            clips[Sound.Explode] = Render("boom", 0.55f, (t, n) =>
            {
                float noise = Noise(t) * Env(n, 0.005f, 0.5f);
                float body = Square(90f - 55f * n, t) * Env(n, 0.005f, 0.3f);
                return Mathf.Clamp(noise * 0.75f + body * 0.5f, -1f, 1f);
            });

            clips[Sound.Pickup] = Render("pick", 0.24f, (t, n) =>
            {
                float step = Mathf.Floor(n * 4f);
                float f = 523f * Mathf.Pow(1.26f, step);
                return Square(f, t, 0.25f) * Env(n, 0.005f, 0.22f) * 0.45f;
            });

            clips[Sound.Death] = Render("die", 0.7f, (t, n) =>
            {
                float f = 700f * Mathf.Pow(0.25f, n);
                return (Square(f, t, 0.5f) * 0.6f + Noise(t) * 0.2f) * Env(n, 0.01f, 0.65f) * 0.5f;
            });

            clips[Sound.Kick] = Render("kick", 0.18f, (t, n) =>
                (Noise(t) * 0.4f + Square(200f + 300f * n, t) * 0.3f) * Env(n, 0.01f, 0.16f) * 0.5f);

            clips[Sound.Menu] = Render("menu", 0.08f, (t, n) =>
                Square(880f, t, 0.5f) * Env(n, 0.005f, 0.07f) * 0.4f);

            clips[Sound.Tick] = Render("tick", 0.09f, (t, n) =>
                Square(1320f, t, 0.5f) * Env(n, 0.002f, 0.06f) * 0.35f);

            clips[Sound.Drop] = Render("drop", 0.22f, (t, n) =>
                (Square(160f - 110f * n, t) * 0.5f + Noise(t) * 0.35f) * Env(n, 0.005f, 0.2f) * 0.6f);

            float[] fan = { 523f, 659f, 784f, 1046f };
            clips[Sound.Fanfare] = Render("win", 0.9f, (t, n) =>
            {
                int i = Mathf.Clamp((int)(n * 5.5f), 0, 3);
                float f = fan[i];
                return (Square(f, t, 0.5f) * 0.4f + Square(f * 2f, t, 0.25f) * 0.15f) * Env(n, 0.01f, 0.88f);
            });

            musicSource.clip = BuildMusic();
        }

        public static void Play(Sound s, float pitch = 1f, float volume = 1f)
        {
            if (DemoMode.Mute) return;
            if (I == null || !I.clips.TryGetValue(s, out var clip)) return;
            I.sfxSource.pitch = pitch;
            I.sfxSource.PlayOneShot(clip, volume);
        }

        public static void Music(bool on)
        {
            if (I == null) return;
            if (DemoMode.Mute) on = false;
            if (on && !I.musicSource.isPlaying) I.musicSource.Play();
            else if (!on && I.musicSource.isPlaying) I.musicSource.Stop();
        }

        // ------------------------------------------------------------ synthesis

        static float Square(float freq, float t, float duty = 0.5f)
        {
            float phase = t * freq;
            return (phase - Mathf.Floor(phase)) < duty ? 1f : -1f;
        }

        static float Tri(float freq, float t)
        {
            float phase = (t * freq) % 1f;
            return 4f * Mathf.Abs(phase - 0.5f) - 1f;
        }

        static float Noise(float t)
        {
            // deterministic hash noise so clips are reproducible
            int n = (int)(t * Rate);
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
        }

        static float Env(float n, float attack, float release)
        {
            if (n < attack) return n / Mathf.Max(attack, 1e-4f);
            float k = Mathf.InverseLerp(attack, release, n);
            return Mathf.Clamp01(1f - k);
        }

        static AudioClip Render(string name, float seconds, System.Func<float, float, float> fn)
        {
            int len = Mathf.CeilToInt(seconds * Rate);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                data[i] = fn(t, i / (float)len);
            }
            var clip = AudioClip.Create(name, len, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // A two bar loop: driving bass, square lead, noise hat.
        static readonly int[] LeadSteps = { 0, 7, 12, 7, 3, 10, 15, 10, 0, 7, 12, 15, 14, 12, 10, 7 };
        static readonly int[] BassSteps = { 0, 0, 5, 5, 3, 3, 7, 7 };

        static AudioClip BuildMusic()
        {
            const float bpm = 150f;
            float step = 60f / bpm / 4f;              // 16th note
            int steps = LeadSteps.Length * 2;
            int len = Mathf.CeilToInt(steps * step * Rate);
            var data = new float[len];

            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                int s = Mathf.Min((int)(t / step), steps - 1);
                float local = t - s * step;
                float n = local / step;

                float lead = Midi(57 + LeadSteps[s % LeadSteps.Length]);
                float bass = Midi(33 + BassSteps[(s / 2) % BassSteps.Length]);

                float v = Square(lead, t, 0.25f) * 0.20f * Env(n, 0.02f, 0.9f);
                v += Tri(bass, t) * 0.26f * Env(n, 0.01f, 0.8f);
                if (s % 2 == 1) v += Noise(t) * 0.06f * Env(n, 0.005f, 0.25f);
                data[i] = Mathf.Clamp(v, -1f, 1f);
            }

            var clip = AudioClip.Create("bgm", len, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);
    }
}
