using System.Collections.Generic;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>The bow after a concert: musicians lean forward, the conductor turns to the audience. Same timing as the web preview.</summary>
    public static class StageBow
    {
        public const float Duration = 3.2f;
        static float start = -100f;
        public static void Begin() { start = Time.time; }
        /// <summary>0 → 1 → 0 over the bow (eased).</summary>
        public static float Weight
        {
            get
            {
                float t = (Time.time - start) / Duration;
                if (t < 0f || t > 1f) return 0f;
                return Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 1.25f)) * (t < 0.8f ? 1f : 1f - (t - 0.8f) / 0.2f);
            }
        }
    }

    /// <summary>House lights dim and the stage brightens while a concert is playing (theatre-style).</summary>
    public class HouseLights : MonoBehaviour
    {
        readonly List<Light> house = new List<Light>(), stage = new List<Light>();
        readonly List<float> houseBase = new List<float>(), stageBase = new List<float>();
        Light key; float keyBase;
        Color sky, equator, ground;
        float dim;

        public void Init(Light keyLight, IEnumerable<Light> stageLights, IEnumerable<Light> houseLights)
        {
            key = keyLight; keyBase = key.intensity;
            foreach (var l in stageLights) { stage.Add(l); stageBase.Add(l.intensity); }
            foreach (var l in houseLights) { house.Add(l); houseBase.Add(l.intensity); }
            sky = RenderSettings.ambientSkyColor; equator = RenderSettings.ambientEquatorColor; ground = RenderSettings.ambientGroundColor;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool concert = gm != null && gm.Performance != null && gm.Performance.IsBusy && gm.Performance.Context == PerformanceContext.Concert;
            float target = concert || StageBow.Weight > 0f ? 1f : 0f;
            if (Mathf.Approximately(dim, target)) return;
            dim = Mathf.MoveTowards(dim, target, Time.deltaTime / 1.5f);
            for (int i = 0; i < house.Count; i++) house[i].intensity = houseBase[i] * Mathf.Lerp(1f, 0.25f, dim);
            for (int i = 0; i < stage.Count; i++) stage[i].intensity = stageBase[i] * Mathf.Lerp(1f, 1.2f, dim);
            key.intensity = keyBase * Mathf.Lerp(1f, 0.6f, dim);
            float a = Mathf.Lerp(1f, 0.55f, dim);
            RenderSettings.ambientSkyColor = sky * a; RenderSettings.ambientEquatorColor = equator * a; RenderSettings.ambientGroundColor = ground * a;
        }
    }
}
