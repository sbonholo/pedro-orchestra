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
        float upperLen, foreLen;
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
            float skinTone = (float)rng.NextDouble();
            var skin = MaterialLibrary.Skin(skinTone);
            var hair = MaterialLibrary.Hair(rng.Next(5));

            bool standing = Def.Posture == PlayingPosture.StandingUpright;
            bool cello = Def.Posture == PlayingPosture.SeatedBetweenKnees;
            float hipY = standing ? 0.95f : 0.52f;
            float shoulderY = standing ? 1.43f : 1.0f;
            upperLen = 0.30f; foreLen = 0.28f;

            // Furniture
            if (standing) PlaceholderFactory.BuildStool(transform, new Vector3(0.05f, 0, -0.35f));
            else PlaceholderFactory.BuildChair(transform, new Vector3(0, 0, -0.02f), 0f);
            float standX = cello ? 0.45f : standing ? 0.55f : 0.35f;
            PlaceholderFactory.BuildMusicStand(transform, new Vector3(standX, 0, 0.95f), 0f, standing ? 1.15f : 0.85f);

            // Same figure as the web preview (identical numbers): tailcoat or concert dress, face, hands with fingers.
            var coat = Data.female ? MaterialLibrary.BlackFabric : MaterialLibrary.Jacket;
            var lip = MaterialLibrary.Lip(skinTone);
            var P = PrimitiveType.Sphere;

            // Legs: trousers (or a long dress) and polished shoes
            float knee = cello ? 0.24f : 0.13f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 hip = new Vector3(s * 0.1f, hipY, 0.02f);
                Vector3 k = standing ? new Vector3(s * 0.12f, 0.5f, 0.04f) : new Vector3(s * knee, 0.52f, 0.44f);
                Vector3 ankle = standing ? new Vector3(s * 0.13f, 0.09f, 0.0f) : new Vector3(s * (knee - 0.01f), 0.09f, 0.5f);
                Limb("Thigh", transform, MaterialLibrary.BlackFabric, hip, k, 0.08f);
                Limb("Shin", transform, MaterialLibrary.BlackFabric, k, ankle, 0.06f);
                PlaceholderFactory.Part("Shoe", transform, P, MaterialLibrary.Shoe, ankle + new Vector3(0, -0.055f, 0.06f), new Vector3(0.1f, 0.065f, 0.27f));
                PlaceholderFactory.Part("Heel", transform, PrimitiveType.Cube, MaterialLibrary.Shoe, ankle + new Vector3(0, -0.07f, -0.05f), new Vector3(0.075f, 0.04f, 0.06f));
            }
            if (Data.female)
            {
                if (standing) PlaceholderFactory.MeshPart("Skirt", transform, PlaceholderFactory.Skirt, MaterialLibrary.BlackFabric, new Vector3(0, 0.52f, 0.02f), new Vector3(0.56f, 0.9f, 0.44f));
                else
                {
                    PlaceholderFactory.Part("Lap", transform, PrimitiveType.Cube, MaterialLibrary.BlackFabric, new Vector3(0, hipY + 0.02f, 0.22f), new Vector3(cello ? 0.5f : 0.36f, 0.12f, 0.5f));
                    PlaceholderFactory.MeshPart("Skirt", transform, PlaceholderFactory.Skirt, MaterialLibrary.BlackFabric, new Vector3(0, 0.3f, 0.47f), new Vector3(cello ? 0.78f : 0.5f, 0.44f, 0.34f));
                }
            }

            // Torso: tapered waist and chest, tailcoat with satin lapels and bow tie, or a concert dress
            spine = PlaceholderFactory.Empty("Spine", transform, new Vector3(0, hipY, 0));
            float th = shoulderY - hipY;
            PlaceholderFactory.Part("Pelvis", spine, PrimitiveType.Capsule, coat, new Vector3(0, 0.04f, 0.01f), new Vector3(0.33f, 0.12f, 0.25f));
            PlaceholderFactory.Part("Waist", spine, PrimitiveType.Capsule, coat, new Vector3(0, th * 0.32f, 0), new Vector3(0.31f, th * 0.3f, 0.21f));
            PlaceholderFactory.Part("Chest", spine, PrimitiveType.Capsule, coat, new Vector3(0, th * 0.68f, 0.005f), new Vector3(0.37f, th * 0.3f, 0.235f));
            if (!Data.female)
            {
                PlaceholderFactory.Part("Shirt", spine, PrimitiveType.Cube, MaterialLibrary.WhiteShirt, new Vector3(0, th * 0.74f, 0.112f), new Vector3(0.1f, th * 0.42f, 0.012f), Quaternion.Euler(-4f, 0, 0));
                for (int s = -1; s <= 1; s += 2)
                    PlaceholderFactory.Part("Lapel", spine, PrimitiveType.Cube, MaterialLibrary.Satin, new Vector3(s * 0.056f, th * 0.72f, 0.116f), new Vector3(0.045f, th * 0.46f, 0.01f), Quaternion.Euler(-4f, 0, -s * 16f));
                PlaceholderFactory.Part("Collar", spine, PrimitiveType.Cylinder, MaterialLibrary.WhiteShirt, new Vector3(0, th + 0.01f, 0), new Vector3(0.12f, 0.025f, 0.12f));
                PlaceholderFactory.Part("Bow Tie", spine, PrimitiveType.Cube, MaterialLibrary.BlackFabric, new Vector3(0, th - 0.005f, 0.122f), new Vector3(0.03f, 0.028f, 0.02f));
                for (int s = -1; s <= 1; s += 2)
                    PlaceholderFactory.Part("Bow Tie Wing", spine, P, MaterialLibrary.BlackFabric, new Vector3(s * 0.028f, th - 0.005f, 0.12f), new Vector3(0.05f, 0.034f, 0.022f));
                if (standing) PlaceholderFactory.Part("Tails", spine, PrimitiveType.Cube, coat, new Vector3(0, -0.14f, -0.12f), new Vector3(0.3f, 0.32f, 0.03f), Quaternion.Euler(6f, 0, 0));
            }
            else
            {
                PlaceholderFactory.Part("Neckline", spine, P, skin, new Vector3(0, th * 0.93f, 0.094f), new Vector3(0.15f, 0.09f, 0.03f));
                PlaceholderFactory.Part("Pendant", spine, P, MaterialLibrary.Pearl, new Vector3(0, th * 0.86f, 0.112f), Vector3.one * 0.018f);
            }
            PlaceholderFactory.Part("Neck", spine, PrimitiveType.Cylinder, skin, new Vector3(0, th + 0.06f, -0.005f), new Vector3(0.085f, 0.055f, 0.085f));

            // Head: skull and jaw, ears, eyes, brows, nose, lips, one of six hairstyles
            head = PlaceholderFactory.Empty("Head", spine, new Vector3(0, th + 0.2f, 0.02f));
            PlaceholderFactory.Part("Skull", head, P, skin, new Vector3(0, 0.015f, -0.01f), new Vector3(0.175f, 0.2f, 0.2f));
            PlaceholderFactory.Part("Jaw", head, P, skin, new Vector3(0, -0.05f, 0.02f), new Vector3(0.135f, 0.13f, 0.15f));
            PlaceholderFactory.Part("Chin", head, P, skin, new Vector3(0, -0.1f, 0.05f), new Vector3(0.06f, 0.045f, 0.055f));
            for (int s = -1; s <= 1; s += 2)
            {
                PlaceholderFactory.Part("Ear", head, P, skin, new Vector3(s * 0.088f, -0.005f, -0.01f), new Vector3(0.022f, 0.05f, 0.032f));
                PlaceholderFactory.Part("Eye", head, P, MaterialLibrary.EyeWhite, new Vector3(s * 0.034f, 0.008f, 0.083f), new Vector3(0.028f, 0.017f, 0.016f));
                PlaceholderFactory.Part("Iris", head, P, MaterialLibrary.Iris, new Vector3(s * 0.034f, 0.008f, 0.09f), new Vector3(0.013f, 0.013f, 0.006f));
                PlaceholderFactory.Part("Brow", head, PrimitiveType.Cube, hair, new Vector3(s * 0.036f, 0.036f, 0.092f), new Vector3(0.042f, 0.008f, 0.01f), Quaternion.Euler(0, 0, -s * 6f));
            }
            PlaceholderFactory.Part("Nose", head, P, skin, new Vector3(0, -0.012f, 0.098f), new Vector3(0.028f, 0.05f, 0.04f));
            PlaceholderFactory.Part("Nose Tip", head, P, skin, new Vector3(0, -0.034f, 0.106f), new Vector3(0.034f, 0.028f, 0.032f));
            PlaceholderFactory.Part("Lips", head, P, lip, new Vector3(0, -0.066f, 0.087f), new Vector3(0.042f, 0.013f, 0.018f));
            int[] maleStyles = { 0, 0, 1, 1, 2 };
            int style = Data.female ? 3 + rng.Next(3) : maleStyles[rng.Next(5)];
            if (style != 2) PlaceholderFactory.Part("Hair", head, P, hair, new Vector3(0, 0.05f, -0.025f), new Vector3(0.185f + (style == 5 ? 0.015f : 0f), 0.165f, 0.2f));
            if (style <= 1) PlaceholderFactory.Part("Hair Back", head, P, hair, new Vector3(0, -0.01f, -0.055f), new Vector3(0.17f, 0.15f, 0.13f));
            if (style == 1) PlaceholderFactory.Part("Fringe", head, P, hair, new Vector3(-0.025f, 0.07f, 0.045f), new Vector3(0.15f, 0.07f, 0.12f), Quaternion.Euler(0, 0, -10f));
            if (style == 2)
            {
                PlaceholderFactory.Part("Hair Ring", head, P, hair, new Vector3(0, -0.02f, -0.06f), new Vector3(0.18f, 0.12f, 0.12f));
                PlaceholderFactory.Part("Beard", head, P, hair, new Vector3(0, -0.075f, 0.045f), new Vector3(0.13f, 0.09f, 0.11f));
                PlaceholderFactory.Part("Moustache", head, PrimitiveType.Cube, hair, new Vector3(0, -0.052f, 0.095f), new Vector3(0.05f, 0.012f, 0.012f));
            }
            if (style == 3)
            {
                PlaceholderFactory.Part("Hair Back", head, P, hair, new Vector3(0, -0.01f, -0.05f), new Vector3(0.18f, 0.15f, 0.14f));
                PlaceholderFactory.Part("Bun", head, P, hair, new Vector3(0, 0.06f, -0.11f), Vector3.one * 0.09f);
            }
            if (style == 4) PlaceholderFactory.Part("Long Hair", head, PrimitiveType.Capsule, hair, new Vector3(0, -0.1f, -0.07f), new Vector3(0.19f, 0.17f, 0.08f));
            if (style == 5) PlaceholderFactory.Part("Bob", head, P, hair, new Vector3(0, -0.035f, -0.02f), new Vector3(0.2f, 0.12f, 0.2f));

            shoulderL = PlaceholderFactory.Empty("Shoulder L", spine, new Vector3(-0.19f, th - 0.02f, 0));
            shoulderR = PlaceholderFactory.Empty("Shoulder R", spine, new Vector3(0.19f, th - 0.02f, 0));
            PlaceholderFactory.Part("Shoulder Pad L", spine, P, coat, new Vector3(-0.18f, th - 0.03f, 0), new Vector3(0.13f, 0.11f, 0.13f));
            PlaceholderFactory.Part("Shoulder Pad R", spine, P, coat, new Vector3(0.18f, th - 0.03f, 0), new Vector3(0.13f, 0.11f, 0.13f));

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

            // Arms (positioned every frame by IK)
            upperArmL = Limb("Upper Arm L", transform, coat, Vector3.zero, Vector3.up, 0.05f);
            foreArmL = Limb("Forearm L", transform, coat, Vector3.zero, Vector3.up, 0.043f);
            upperArmR = Limb("Upper Arm R", transform, coat, Vector3.zero, Vector3.up, 0.05f);
            foreArmR = Limb("Forearm R", transform, coat, Vector3.zero, Vector3.up, 0.043f);
            var cuff = Data.female ? skin : MaterialLibrary.WhiteShirt;
            handL = BuildHand("Hand L", skin, cuff, 55f);
            handR = BuildHand("Hand R", skin, cuff, 35f);

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

            SolveArm(shoulderL.position, leftTarget, transform.TransformDirection(poleL), upperArmL, foreArmL, handL);
            SolveArm(shoulderR.position, rightTarget, transform.TransformDirection(poleR), upperArmR, foreArmR, handR);
        }

        void SolveArm(Vector3 shoulder, Vector3 target, Vector3 pole, Transform upper, Transform fore, Transform hand)
        {
            float a = upperLen, b = foreLen;
            Vector3 toT = target - shoulder;
            float d = toT.magnitude;
            if (d > a + b - 0.001f) { float k = d / (a + b - 0.001f); a *= k; b *= k; } // stretch rather than detach
            d = Mathf.Max(d, Mathf.Abs(a - b) + 0.001f);
            Vector3 dir = toT.normalized;
            float cosA = Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f);
            Vector3 bend = Vector3.ProjectOnPlane(pole, dir).normalized;
            if (bend.sqrMagnitude < 0.001f) bend = Vector3.down;
            Vector3 elbow = shoulder + dir * (cosA * a) + bend * (Mathf.Sqrt(1f - cosA * cosA) * a);
            PlaceLimb(upper, shoulder, elbow);
            PlaceLimb(fore, elbow, target);
            hand.position = target;
            hand.rotation = Quaternion.LookRotation(target - elbow, pole);
        }

        /// <summary>Palm, four two-segment fingers and a thumb; +Z points along the forearm. curl = finger bend in degrees.</summary>
        Transform BuildHand(string name, Material skin, Material cuff, float curl)
        {
            var g = PlaceholderFactory.Empty(name, transform, Vector3.zero);
            PlaceholderFactory.Part("Cuff", g, PrimitiveType.Cylinder, cuff, new Vector3(0, 0, -0.03f), new Vector3(0.05f, 0.012f, 0.05f), Quaternion.Euler(90f, 0, 0));
            PlaceholderFactory.Part("Palm", g, PrimitiveType.Sphere, skin, new Vector3(0, 0, 0.045f), new Vector3(0.075f, 0.03f, 0.09f));
            float c = curl * Mathf.Deg2Rad;
            for (int i = 0; i < 4; i++)
            {
                float x = -0.027f + i * 0.018f, len = i == 0 || i == 3 ? 0.019f : 0.023f;
                PlaceholderFactory.Part("Finger", g, PrimitiveType.Capsule, skin, new Vector3(x, -0.004f, 0.095f), new Vector3(0.017f, len, 0.017f), Quaternion.Euler(90f + curl * 0.5f, 0, 0));
                PlaceholderFactory.Part("Fingertip", g, PrimitiveType.Capsule, skin, new Vector3(x, -0.004f - Mathf.Sin(c) * 0.03f, 0.12f - (1f - Mathf.Cos(c)) * 0.03f), new Vector3(0.015f, len * 0.8f, 0.015f), Quaternion.Euler(90f + curl * 1.3f, 0, 0));
            }
            PlaceholderFactory.Part("Thumb", g, PrimitiveType.Capsule, skin, new Vector3(0.042f, -0.006f, 0.045f), new Vector3(0.02f, 0.022f, 0.02f), Quaternion.Euler(70f, 0, -35f));
            return g;
        }

        static Transform Limb(string name, Transform parent, Material mat, Vector3 from, Vector3 to, float radius)
        {
            var t = PlaceholderFactory.Part(name, parent, PrimitiveType.Capsule, mat, Vector3.zero, Vector3.one);
            t.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
            PlaceLimbLocal(t, from, to);
            return t;
        }

        static void PlaceLimbLocal(Transform t, Vector3 from, Vector3 to)
        {
            t.localPosition = (from + to) * 0.5f;
            t.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
            var s = t.localScale; s.y = Mathf.Max(0.01f, (to - from).magnitude * 0.5f + s.x * 0.25f); t.localScale = s;
        }

        static void PlaceLimb(Transform t, Vector3 fromWorld, Vector3 toWorld)
        {
            t.position = (fromWorld + toWorld) * 0.5f;
            t.rotation = Quaternion.FromToRotation(Vector3.up, toWorld - fromWorld);
            var s = t.localScale; s.y = Mathf.Max(0.01f, (toWorld - fromWorld).magnitude * 0.5f + s.x * 0.25f); t.localScale = s;
        }
    }
}
