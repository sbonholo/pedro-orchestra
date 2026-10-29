using System.Collections.Generic;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// Seated audience that fills the hall as reputation grows (10 % at reputation 0, full at 75),
    /// and applause after concerts (louder and longer for more stars and a fuller hall).
    /// The applause is generated noise bursts, so no recording or licence is needed.
    /// Same rules as buildAudience() / applause() in the web preview.
    /// </summary>
    public class AudienceCrowd : MonoBehaviour
    {
        public static AudienceCrowd Instance { get; private set; }
        const int Buckets = 10;
        readonly List<GameObject> buckets = new List<GameObject>();
        AudioSource source;
        int shown = -1;

        public static float Fill(float reputation) { return Economy.AudienceFill(reputation); }

        public void Build(List<Vector3> seatFloors)
        {
            Instance = this;
            var rng = new System.Random(99);
            var order = new List<Vector3>(seatFloors);
            for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = order[i]; order[i] = order[j]; order[j] = t; }   // spread the fill over the hall

            Color[] clothes = { new Color(0.11f, 0.13f, 0.19f), new Color(0.23f, 0.12f, 0.14f), new Color(0.18f, 0.18f, 0.18f), new Color(0.28f, 0.25f, 0.18f), new Color(0.14f, 0.2f, 0.16f), new Color(0.35f, 0.29f, 0.23f) };
            float[] skinTones = { 0.05f, 0.3f, 0.6f, 0.9f };
            int per = Mathf.CeilToInt(order.Count / (float)Buckets);
            for (int k = 0; k < Buckets; k++)
            {
                var b = new BatchBuilder();
                for (int i = k * per; i < Mathf.Min(order.Count, (k + 1) * per); i++)
                {
                    var p = order[i];
                    int ci = rng.Next(clothes.Length);
                    var cloth = MaterialLibrary.Create("Audience" + ci, clothes[ci], 0.15f);
                    b.Sphere(cloth, p + new Vector3(0, 0.78f, -0.02f), new Vector3(0.36f, 0.48f, 0.26f));
                    b.Sphere(MaterialLibrary.Skin(skinTones[rng.Next(skinTones.Length)]), p + new Vector3(0, 1.16f, 0.01f), new Vector3(0.19f, 0.23f, 0.21f));
                }
                var go = b.Build("Audience " + (k + 1), transform, castShadows: false);
                buckets.Add(go);
            }
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            float rep = gm != null && gm.State != null ? gm.State.reputation : 0f;
            int n = Mathf.RoundToInt(Fill(rep) * Buckets);
            if (n == shown) return;
            shown = n;
            for (int i = 0; i < buckets.Count; i++) if (buckets[i] != null) buckets[i].SetActive(i < n);
        }

        public void Applaud(int stars, float reputation)
        {
            int sr = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            var data = Applause(stars, Fill(reputation), sr, new System.Random(System.Environment.TickCount));
            var clip = AudioClip.Create("Applause", data.Length, 1, sr, false);
            clip.SetData(data, 0);
            if (source.clip != null) Destroy(source.clip);
            source.clip = clip;
            source.Play();
        }

        /// <summary>Many short noise bursts ("claps"), band-limited around 1.5 kHz, fading out.</summary>
        public static float[] Applause(int stars, float fill, int sampleRate, System.Random rng)
        {
            float dur = 2.2f + stars * 0.9f;
            int claps = Mathf.RoundToInt((60f + 340f * fill) * (0.4f + stars * 0.15f) * dur);
            var y = new float[(int)((dur + 0.2f) * sampleRate)];
            float gain = 0.5f * (0.5f + 0.1f * stars);
            for (int c = 0; c < claps; c++)
            {
                float t = Mathf.Pow((float)rng.NextDouble(), 1.3f) * dur;
                float lvl = (0.04f + (float)rng.NextDouble() * 0.06f) * (1f - 0.6f * t / dur) * gain;
                float decay = 0.03f + (float)rng.NextDouble() * 0.03f;
                int start = (int)(t * sampleRate), len = (int)(0.07f * sampleRate);
                float lp = 0f, hp = 0f, prev = 0f;
                for (int i = 0; i < len && start + i < y.Length; i++)
                {
                    float n = (float)rng.NextDouble() * 2f - 1f;
                    lp += (n - lp) * 0.45f;                 // low-pass ~ 4 kHz
                    hp = 0.9f * (hp + lp - prev); prev = lp; // high-pass ~ 700 Hz
                    y[start + i] += hp * lvl * 6f * Mathf.Exp(-(i / (float)sampleRate) / (decay * 0.35f));
                }
            }
            for (int i = 0; i < y.Length; i++) y[i] = Mathf.Clamp(y[i], -0.95f, 0.95f);
            return y;
        }
    }
}
