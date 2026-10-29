using UnityEngine;

namespace OrchestraProject
{
    /// <summary>The parts of a built figure that animation needs.</summary>
    public class Figure
    {
        public Transform spine, head, shoulderL, shoulderR, upperArmL, foreArmL, handL, upperArmR, foreArmR, handR;
        public float hipY, shoulderY;
        public Material skin, hair;
    }

    /// <summary>
    /// Human figure shared by musicians and the conductor (same numbers as buildFigure() in the web preview):
    /// legs, tailcoat or concert dress, head and face, shoulders, arms and hands with fingers. Built from primitives.
    /// </summary>
    public static class HumanFigure
    {
        public const float UpperArm = 0.30f, ForeArm = 0.28f;

        public static Figure Build(Transform root, bool female, System.Random rng, bool standing, bool cello)
        {
            var f = new Figure();
            float skinTone = (float)rng.NextDouble();
            var skin = f.skin = MaterialLibrary.Skin(skinTone);
            var hair = f.hair = MaterialLibrary.Hair(rng.Next(5));
            float hipY = f.hipY = standing ? 0.95f : 0.52f;
            float shoulderY = f.shoulderY = standing ? 1.43f : 1.0f;

            // Same figure as the web preview (identical numbers): tailcoat or concert dress, face, hands with fingers.
            var coat = female ? MaterialLibrary.BlackFabric : MaterialLibrary.Jacket;
            var lip = MaterialLibrary.Lip(skinTone);
            var P = PrimitiveType.Sphere;

            // Legs: trousers (or a long dress) and polished shoes
            float knee = cello ? 0.24f : 0.13f;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 hip = new Vector3(s * 0.1f, hipY, 0.02f);
                Vector3 k = standing ? new Vector3(s * 0.12f, 0.5f, 0.04f) : new Vector3(s * knee, 0.52f, 0.44f);
                Vector3 ankle = standing ? new Vector3(s * 0.13f, 0.09f, 0.0f) : new Vector3(s * (knee - 0.01f), 0.09f, 0.5f);
                Limb("Thigh", root, MaterialLibrary.BlackFabric, hip, k, 0.08f);
                Limb("Shin", root, MaterialLibrary.BlackFabric, k, ankle, 0.06f);
                PlaceholderFactory.Part("Shoe", root, P, MaterialLibrary.Shoe, ankle + new Vector3(0, -0.055f, 0.06f), new Vector3(0.1f, 0.065f, 0.27f));
                PlaceholderFactory.Part("Heel", root, PrimitiveType.Cube, MaterialLibrary.Shoe, ankle + new Vector3(0, -0.07f, -0.05f), new Vector3(0.075f, 0.04f, 0.06f));
            }
            if (female)
            {
                if (standing) PlaceholderFactory.MeshPart("Skirt", root, PlaceholderFactory.Skirt, MaterialLibrary.BlackFabric, new Vector3(0, 0.52f, 0.02f), new Vector3(0.56f, 0.9f, 0.44f));
                else
                {
                    PlaceholderFactory.Part("Lap", root, PrimitiveType.Cube, MaterialLibrary.BlackFabric, new Vector3(0, hipY + 0.02f, 0.22f), new Vector3(cello ? 0.5f : 0.36f, 0.12f, 0.5f));
                    PlaceholderFactory.MeshPart("Skirt", root, PlaceholderFactory.Skirt, MaterialLibrary.BlackFabric, new Vector3(0, 0.3f, 0.47f), new Vector3(cello ? 0.78f : 0.5f, 0.44f, 0.34f));
                }
            }

            // Torso: tapered waist and chest, tailcoat with satin lapels and bow tie, or a concert dress
            var spine = f.spine = PlaceholderFactory.Empty("Spine", root, new Vector3(0, hipY, 0));
            float th = shoulderY - hipY;
            PlaceholderFactory.Part("Pelvis", spine, PrimitiveType.Capsule, coat, new Vector3(0, 0.04f, 0.01f), new Vector3(0.33f, 0.12f, 0.25f));
            PlaceholderFactory.Part("Waist", spine, PrimitiveType.Capsule, coat, new Vector3(0, th * 0.32f, 0), new Vector3(0.31f, th * 0.3f, 0.21f));
            PlaceholderFactory.Part("Chest", spine, PrimitiveType.Capsule, coat, new Vector3(0, th * 0.68f, 0.005f), new Vector3(0.37f, th * 0.3f, 0.235f));
            if (!female)
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
            var head = f.head = PlaceholderFactory.Empty("Head", spine, new Vector3(0, th + 0.2f, 0.02f));
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
            int style = female ? 3 + rng.Next(3) : maleStyles[rng.Next(5)];
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

            f.shoulderL = PlaceholderFactory.Empty("Shoulder L", spine, new Vector3(-0.19f, th - 0.02f, 0));
            f.shoulderR = PlaceholderFactory.Empty("Shoulder R", spine, new Vector3(0.19f, th - 0.02f, 0));
            PlaceholderFactory.Part("Shoulder Pad L", spine, P, coat, new Vector3(-0.18f, th - 0.03f, 0), new Vector3(0.13f, 0.11f, 0.13f));
            PlaceholderFactory.Part("Shoulder Pad R", spine, P, coat, new Vector3(0.18f, th - 0.03f, 0), new Vector3(0.13f, 0.11f, 0.13f));

            // Arms (positioned every frame by IK)
            f.upperArmL = Limb("Upper Arm L", root, coat, Vector3.zero, Vector3.up, 0.05f);
            f.foreArmL = Limb("Forearm L", root, coat, Vector3.zero, Vector3.up, 0.043f);
            f.upperArmR = Limb("Upper Arm R", root, coat, Vector3.zero, Vector3.up, 0.05f);
            f.foreArmR = Limb("Forearm R", root, coat, Vector3.zero, Vector3.up, 0.043f);
            var cuff = female ? skin : MaterialLibrary.WhiteShirt;
            f.handL = BuildHand(root, "Hand L", skin, cuff, 55f);
            f.handR = BuildHand(root, "Hand R", skin, cuff, 35f);

            return f;
        }

        /// <summary>Two-bone IK: places upper arm, forearm and hand so the hand reaches target; the elbow bends toward pole.</summary>
        public static void SolveArm(Vector3 shoulder, Vector3 target, Vector3 pole, Transform upper, Transform fore, Transform hand)
        {
            float a = UpperArm, b = ForeArm;
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
        static Transform BuildHand(Transform root, string name, Material skin, Material cuff, float curl)
        {
            var g = PlaceholderFactory.Empty(name, root, Vector3.zero);
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

        public static Transform Limb(string name, Transform parent, Material mat, Vector3 from, Vector3 to, float radius)
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

