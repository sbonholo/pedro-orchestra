using System.Collections.Generic;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// The 3D musician: procedural figure (tailcoat or dress, face, hands) built from primitives + instrument + procedural playing animation.
    /// The bow follows the exact notes being heard (down-bow / up-bow alternate per note, bow speed
    /// follows note length, string crossings follow pitch), the left hand shifts position with pitch
    /// and shows vibrato. Arms use two-bone IK so they stay attached to bow and neck.
    /// Stage 3 swaps the mannequin for a rigged human model driven by the same note data.
    /// </summary>
    public class MusicianView : MonoBehaviour
    {
        public MusicianData Data { get; private set; }
        public InstrumentDefinition Def { get; private set; }
        public AudioSource Audio { get; private set; }
        public bool IsPerforming { get { return notes != null; } }

        Transform spine, head, instrument, bow, shoulderL, shoulderR, ring;
        Transform upperArmL, foreArmL, handL, upperArmR, foreArmR, handR;
        Vector3 bowContactLocal;
        float nutZ, neckBodyZ;
        Vector3 poleL, poleR;

        List<RenderedNote> notes;
        double startDsp;
        int noteIndex;
        float bowPos, strokeFrom, strokeTo;
        int strokeNote = -1;
        float lift = 1f;
        float idlePhase;

        public void Build(MusicianData data)
        {
            Data = data;
            Def = data.Instrument;
            idlePhase = (data.appearanceSeed % 1000) * 0.01f;
            var rng = new System.Random(data.appearanceSeed);
            bool standing = Def.Posture == PlayingPosture.StandingUpright;
            bool cello = Def.Posture == PlayingPosture.SeatedBetweenKnees;

            // Furniture
            if (standing) PlaceholderFactory.BuildStool(transform, new Vector3(0.05f, 0, -0.35f));
            else PlaceholderFactory.BuildChair(transform, new Vector3(0, 0, -0.02f), 0f);
            float standX = cello ? 0.45f : standing ? 0.55f : 0.35f;
            PlaceholderFactory.BuildMusicStand(transform, new Vector3(standX, 0, 0.95f), 0f, standing ? 1.15f : 0.85f);

            var fig = HumanFigure.Build(transform, Data.female, rng, standing, cello);
            spine = fig.spine; head = fig.head; shoulderL = fig.shoulderL; shoulderR = fig.shoulderR;
            upperArmL = fig.upperArmL; foreArmL = fig.foreArmL; handL = fig.handL;
            upperArmR = fig.upperArmR; foreArmR = fig.foreArmR; handR = fig.handR;
            float shoulderY = fig.shoulderY;

            // Instrument in playing position
            Vector3 instPos, instFwd, instUp;
            Transform instParent;
            switch (Def.Posture)
            {
                case PlayingPosture.SeatedShoulder:
                    instParent = spine;
                    instPos = new Vector3(-0.03f, shoulderY + 0.02f, 0.07f) - spine.localPosition;
                    instFwd = new Vector3(-0.48f, 0.06f, 0.88f);
                    instUp = new Vector3(0.55f, 0.83f, 0.05f);
                    head.localRotation = Quaternion.Euler(12f, -10f, 14f);
                    poleL = new Vector3(0.15f, -1f, 0.2f);
                    poleR = new Vector3(1f, -0.7f, -0.3f);
                    break;
                case PlayingPosture.SeatedBetweenKnees:
                    instParent = transform;
                    instPos = new Vector3(0f, 0f, 0.50f);
                    instFwd = new Vector3(-0.10f, 1f, -0.38f);
                    instUp = new Vector3(0.05f, 0.3f, 1f);
                    head.localRotation = Quaternion.Euler(6f, 8f, -6f);
                    poleL = new Vector3(-1f, -0.2f, 0.2f);
                    poleR = new Vector3(1f, -0.6f, -0.4f);
                    break;
                default:
                    instParent = transform;
                    instPos = new Vector3(-0.10f, 0f, 0.42f);
                    instFwd = new Vector3(-0.18f, 1f, -0.22f);
                    instUp = new Vector3(0.12f, 0.2f, 1f);
                    head.localRotation = Quaternion.Euler(8f, 12f, -4f);
                    poleL = new Vector3(-1f, 0f, 0.3f);
                    poleR = new Vector3(1f, -0.5f, -0.5f);
                    break;
            }
            instrument = PlaceholderFactory.BuildInstrument(Def, instParent, out bowContactLocal, out nutZ, out neckBodyZ);
            instrument.localPosition = instPos;
            instrument.localRotation = Quaternion.LookRotation(instFwd.normalized, instUp.normalized);
            bow = PlaceholderFactory.BuildBow(Def, transform);

            // Selection ring + click collider + spatial audio source
            ring = PlaceholderFactory.Part("Selection Ring", transform, PrimitiveType.Cylinder, MaterialLibrary.SelectionRing, new Vector3(0, 0.005f, 0.2f), new Vector3(1.3f, 0.004f, 1.3f));
            ring.gameObject.SetActive(false);
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.8f, 0.2f); col.radius = 0.5f; col.height = 1.8f;

            Audio = gameObject.AddComponent<AudioSource>();
            Audio.playOnAwake = false;
            Audio.spatialBlend = 0.7f;
            Audio.rolloffMode = AudioRolloffMode.Logarithmic;
            Audio.minDistance = 3f;
            Audio.maxDistance = 60f;
            Audio.dopplerLevel = 0f;
            Audio.spread = 70f;

            bowPos = (Def.BowMin + Def.BowMax) * 0.5f;
            LateUpdate();
        }

        public void SetSelected(bool on) { if (ring != null) ring.gameObject.SetActive(on); }

        public void BeginPerformance(List<RenderedNote> playedNotes, double dspStartOfBeatZero)
        {
            notes = playedNotes;
            startDsp = dspStartOfBeatZero;
            noteIndex = 0;
            strokeNote = -1;
        }

        public void EndPerformance() { notes = null; }

        /// <summary>World-space point to aim the close-up camera at.</summary>
        public Vector3 FocusPoint { get { return transform.TransformPoint(new Vector3(0, Def.Posture == PlayingPosture.StandingUpright ? 1.25f : 0.95f, 0.25f)); } }

        void LateUpdate()
        {
            if (spine == null) return;
            float time = Time.time + idlePhase;
            float t = notes != null ? (float)(AudioSettings.dspTime - startDsp) : -1f;
            RenderedNote current = null;
            float p = 0f;
            bool sounding = false;

            if (notes != null)
            {
                while (noteIndex < notes.Count - 1 && t >= notes[noteIndex + 1].StartSec) noteIndex++;
                if (notes.Count > 0 && t >= notes[noteIndex].StartSec)
                {
                    current = notes[noteIndex];
                    p = Mathf.Clamp01((float)((t - current.StartSec) / current.DurationSec));
                    sounding = t <= current.StartSec + current.DurationSec;
                }
                else if (notes.Count > 0) current = notes[0];
            }

            // Body: breathing when idle, musical sway when playing (more for musical players).
            float musical = Data.Get01(SkillType.Musicality);
            float amp = notes != null ? 1.2f + 3.0f * musical : 0.5f;
            spine.localRotation = Quaternion.Euler(Mathf.Sin(time * 0.9f) * amp, Mathf.Sin(time * 0.43f) * amp * 0.6f, Mathf.Sin(time * 0.61f) * amp * 0.7f);

            // Bow stroke for the current note.
            if (current != null && noteIndex != strokeNote && t >= current.StartSec)
            {
                strokeNote = noteIndex;
                float length = Mathf.Clamp((float)current.DurationSec * (0.22f + 0.25f * current.Velocity), 0.08f, Def.BowMax - Def.BowMin);
                strokeFrom = bowPos;
                strokeTo = current.DownBow ? Mathf.Min(Def.BowMax, bowPos + length) : Mathf.Max(Def.BowMin, bowPos - length);
                if (Mathf.Abs(strokeTo - strokeFrom) < 0.05f) strokeTo = current.DownBow ? Def.BowMin + length : Def.BowMax - length;
            }
            if (sounding)
            {
                bowPos = Mathf.Lerp(strokeFrom, strokeTo, Mathf.SmoothStep(0f, 1f, p));
                lift = Mathf.MoveTowards(lift, 0f, Time.deltaTime * 12f);
            }
            else
            {
                if (notes == null) bowPos = Mathf.MoveTowards(bowPos, (Def.BowMin + Def.BowMax) * 0.5f, Time.deltaTime * 0.2f);
                lift = Mathf.MoveTowards(lift, notes != null ? 0.3f : 1f, Time.deltaTime * 4f);
            }

            // Pitch → string crossing angle and left-hand position.
            float pitch = current != null ? current.PlayedMidi : Def.CenterMidi;
            float pitchNorm = Mathf.Clamp01((pitch - Def.LowestMidi) / (float)(Def.HighestMidi - Def.LowestMidi));
            float handNorm = Mathf.Clamp01((pitch - (Def.LowestMidi + 10)) / 22f);

            Vector3 across = instrument.right;
            if (Vector3.Dot(across, transform.right) < 0f) across = -across;
            across = Quaternion.AngleAxis(Mathf.Lerp(16f, -16f, pitchNorm), instrument.forward) * across;
            Vector3 contact = instrument.TransformPoint(bowContactLocal) + instrument.up * (0.03f * lift);
            Vector3 frog = contact + across * bowPos;
            bow.position = frog;
            bow.rotation = Quaternion.LookRotation(-across, instrument.up);

            // Left hand: position on the neck + vibrato on sustained notes.
            float vib = 0f;
            if (sounding && current.VibratoCents > 1f && t - current.StartSec > current.VibratoDelay)
                vib = Mathf.Sin((float)(t * current.VibratoRate * Mathf.PI * 2f)) * current.VibratoCents / 18f * 0.006f * (Def.BodyLength / 0.36f);
            float handZ = Mathf.Lerp(nutZ - 0.03f * (Def.BodyLength / 0.36f), neckBodyZ + 0.02f, handNorm) + vib;
            Vector3 leftTarget = instrument.TransformPoint(new Vector3(-0.012f, -0.012f, handZ));
            Vector3 rightTarget = frog + bow.up * 0.02f;

            HumanFigure.SolveArm(shoulderL.position, leftTarget, transform.TransformDirection(poleL), upperArmL, foreArmL, handL);
            HumanFigure.SolveArm(shoulderR.position, rightTarget, transform.TransformDirection(poleR), upperArmR, foreArmR, handR);
        }
    }
}
