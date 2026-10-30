using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// The conductor on the podium. Beats time (2/4, 3/4, 4/4 patterns) in sync with the music that is actually
    /// playing: raises the arms before the first beat, rebounds after each beat, cuts off at the end.
    /// Same motion as animateConductor() in the web preview.
    /// </summary>
    public class ConductorView : MonoBehaviour
    {
        PerformanceController perf;
        Figure fig;
        GameObject body;

        static readonly Vector3[] Pattern2 = { new Vector3(0, -0.22f, 0), new Vector3(0.02f, 0.1f, 0) };
        static readonly Vector3[] Pattern3 = { new Vector3(0, -0.22f, 0), new Vector3(0.22f, -0.12f, 0.02f), new Vector3(0.04f, 0.1f, 0) };
        static readonly Vector3[] Pattern4 = { new Vector3(0, -0.22f, 0), new Vector3(-0.18f, -0.12f, 0.02f), new Vector3(0.22f, -0.12f, 0.02f), new Vector3(0.04f, 0.1f, 0) };

        public void Build(PerformanceController performance)
        {
            perf = performance;
            body = new GameObject("Figure");
            body.transform.SetParent(transform, false);
            fig = HumanFigure.Build(body.transform, false, new System.Random(1873), true, false);
            PlaceholderFactory.Part("Baton", fig.handR, PrimitiveType.Cylinder, MaterialLibrary.WhiteShirt, new Vector3(0, 0, 0.26f), new Vector3(0.007f, 0.17f, 0.007f), Quaternion.Euler(90f, 0, 0));
            PlaceholderFactory.Part("Contact Shadow", transform, PrimitiveType.Quad, MaterialLibrary.ContactShadow, new Vector3(0, 0.002f, 0), new Vector3(0.8f, 0.8f, 1f), Quaternion.Euler(90f, 0, 0)).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            transform.localPosition = ConcertHallBuilder.ConductorPoint + new Vector3(0, 0.21f, 0);
            transform.localRotation = Quaternion.identity;   // faces +Z, toward the musicians
        }

        void LateUpdate()
        {
            if (fig == null) return;
            bool audition = perf.IsBusy && (perf.Context == PerformanceContext.Audition || perf.Mode == ArrangementMode.Solo);   // leaves the stage to the soloist
            body.SetActive(!audition);
            if (audition) return;

            float time = Time.time;
            Vector3 right, left;
            float nod = 0f;
            if (perf.IsBusy && perf.HasTiming && perf.PlayerCount > 1)   // a lone soloist plays without a conductor
            {
                double now = AudioSettings.dspTime;
                int bpb = perf.BeatsPerBar;
                var pat = bpb == 2 ? Pattern2 : bpb == 3 ? Pattern3 : Pattern4;
                bpb = pat.Length;
                float bt = (float)((now - perf.BeatZeroDsp) / perf.SecondsPerBeat);
                if (perf.PlayerConducting)   // "You conduct": the baton strikes each beat when the player taps
                    bt = perf.ConductTaps == 0 ? -1f : perf.ConductTaps - 1 + Mathf.Min(0.999f, (float)((now - perf.LastConductTapDsp) / perf.SecondsPerBeat));
                var b0 = new Vector3(0.12f, 1.45f, 0.38f);
                if (bt < 0f) { right = b0 + new Vector3(0.02f, 0.14f, 0); left = new Vector3(-0.16f, 1.55f, 0.35f); }   // preparation
                else
                {
                    // count bars from the first downbeat (pickups can be fractional, e.g. Spring's half-beat upbeat)
                    float x = bt - (perf.PlayerConducting ? Mathf.Floor(perf.PickupBeats) : perf.PickupBeats);
                    int i = Mathf.FloorToInt(x);
                    float fr = x - i;
                    int k = ((i % bpb) + bpb) % bpb;
                    Vector3 off = Vector3.Lerp(pat[k], pat[(k + 1) % bpb], fr * fr * (3f - 2f * fr));
                    off.y += 0.09f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, fr * 1.6f));        // rebound after each beat
                    if (!perf.PlayerConducting && now > perf.MusicEndDsp - 0.2) off *= Mathf.Max(0f, (float)((perf.MusicEndDsp - now) / 0.2));   // cut-off
                    right = b0 + off;
                    left = new Vector3(-0.12f - off.x * 0.5f, 1.36f + off.y * 0.5f, 0.34f);
                    nod = Mathf.Sin(fr * Mathf.PI) * 3f;
                }
            }
            else
            {
                right = new Vector3(0.24f, 0.98f + Mathf.Sin(time * 0.9f) * 0.005f, 0.06f);
                left = new Vector3(-0.24f, 0.98f, 0.06f);
            }
            // After a concert the conductor turns to the audience and bows.
            float bow = StageBow.Weight;
            body.transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(0f, 180f, Mathf.Clamp01(bow * 1.6f)), 0);
            if (bow > 0f) { right = new Vector3(0.24f, 0.98f, 0.06f); left = new Vector3(-0.24f, 0.98f, 0.06f); }
            fig.spine.localRotation = Quaternion.Euler(2f + Mathf.Sin(time * 0.7f) * 0.6f + bow * 35f, 0, Mathf.Sin(time * 0.5f) * 0.4f);
            fig.head.localRotation = Quaternion.Euler(8f + nod, 0, 0);
            var root = body.transform;
            HumanFigure.SolveArm(fig.shoulderR.position, root.TransformPoint(right), root.TransformDirection(new Vector3(1f, -0.6f, -0.3f)), fig.upperArmR, fig.foreArmR, fig.handR);
            HumanFigure.SolveArm(fig.shoulderL.position, root.TransformPoint(left), root.TransformDirection(new Vector3(-1f, -0.6f, -0.3f)), fig.upperArmL, fig.foreArmL, fig.handL);
        }
    }
}
