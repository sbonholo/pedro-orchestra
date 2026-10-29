using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// Builds PLACEHOLDER 3D models from primitives: instruments, bows, chairs, stands and a
    /// mannequin-style musician. Proportions use real measurements (e.g. violin body 35.6 cm,
    /// cello body 76 cm) so swapping in real models later keeps the same scale and hold.
    /// </summary>
    public static class PlaceholderFactory
    {
        // CreatePrimitive needs these classes at runtime. Referencing them stops iOS engine-code
        // stripping from removing them (documented Unity requirement).
#pragma warning disable 0169
        static BoxCollider keepBox; static SphereCollider keepSphere; static CapsuleCollider keepCapsule;
        static MeshCollider keepMesh; static MeshFilter keepFilter; static MeshRenderer keepRenderer;
#pragma warning restore 0169

        public static Transform Part(string name, Transform parent, PrimitiveType type, Material mat,
            Vector3 localPos, Vector3 localScale, Quaternion? localRot = null, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider) { var c = go.GetComponent<Collider>(); if (c != null) Object.Destroy(c); }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        /// <summary>Like Part, but with a custom mesh (e.g. the tapered skirt).</summary>
        public static Transform MeshPart(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3 localScale, Quaternion? localRot = null)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            return go.transform;
        }

        static Mesh skirt;
        /// <summary>Closed cone section, height 1, top radius 0.34, bottom radius 0.5 (same as the web preview's skirt).</summary>
        public static Mesh Skirt
        {
            get
            {
                if (skirt != null) return skirt;
                const int n = 24; const float rt = 0.34f, rb = 0.5f;
                var v = new System.Collections.Generic.List<Vector3>(); var tri = new System.Collections.Generic.List<int>();
                for (int i = 0; i <= n; i++)
                {
                    float a = i * Mathf.PI * 2f / n, c = Mathf.Cos(a), s = Mathf.Sin(a);
                    v.Add(new Vector3(c * rt, 0.5f, s * rt)); v.Add(new Vector3(c * rb, -0.5f, s * rb));
                }
                for (int i = 0; i < n; i++) { int k = i * 2; tri.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 }); }
                int top = v.Count; v.Add(new Vector3(0, 0.5f, 0)); int bottom = v.Count; v.Add(new Vector3(0, -0.5f, 0));
                for (int i = 0; i < n; i++) { int k = i * 2; tri.AddRange(new[] { top, k + 2, k }); tri.AddRange(new[] { bottom, k + 1, k + 3 }); }
                skirt = new Mesh { name = "Skirt" };
                skirt.SetVertices(v); skirt.SetTriangles(tri, 0); skirt.RecalculateNormals(); skirt.RecalculateBounds();
                return skirt;
            }
        }

        public static Transform Empty(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        /// <summary>
        /// Bowed string instrument. Local space: origin = bottom (end button or endpin tip),
        /// +Z = along the neck toward the scroll, +Y = out of the top plate, +X = across the strings.
        /// </summary>
        public static Transform BuildInstrument(InstrumentDefinition d, Transform parent, out Vector3 bowContactLocal, out float nutZ, out float neckBodyZ)
        {
            var root = new GameObject(d.DisplayName).transform;
            root.SetParent(parent, false);
            float e = d.EndpinLength, L = d.BodyLength, W = d.BodyWidth, D = d.BodyDepth;
            var varnish = d.Type == InstrumentType.DoubleBass || d.Type == InstrumentType.Viola ? MaterialLibrary.VarnishDark : MaterialLibrary.Varnish;

            if (e > 0f)
                Part("Endpin", root, PrimitiveType.Cylinder, MaterialLibrary.StringMetal, new Vector3(0, -D * 0.1f, e * 0.5f), new Vector3(0.012f, e * 0.5f, 0.012f), Quaternion.Euler(90, 0, 0));

            // Body: lower bout, upper bout, waist (C-bouts), ribs.
            Part("Lower Bout", root, PrimitiveType.Sphere, varnish, new Vector3(0, 0, e + L * 0.28f), new Vector3(W, D, L * 0.58f));
            Part("Upper Bout", root, PrimitiveType.Sphere, varnish, new Vector3(0, 0, e + L * 0.74f), new Vector3(W * 0.82f, D, L * 0.50f));
            Part("Waist", root, PrimitiveType.Cube, varnish, new Vector3(0, 0, e + L * 0.5f), new Vector3(W * 0.62f, D * 0.9f, L * 0.3f));

            float top = D * 0.5f;
            float bridgeZ = e + L * 0.55f;
            float bridgeH = L * (d.Type == InstrumentType.Violin || d.Type == InstrumentType.Viola ? 0.09f : 0.12f);
            float fbStart = e + L * 0.66f;
            neckBodyZ = e + L;
            nutZ = e + L + d.NeckLength * 0.58f;
            float scrollZ = e + L + d.NeckLength * 0.85f;

            // f-holes
            for (int s = -1; s <= 1; s += 2)
                Part("f-hole", root, PrimitiveType.Cube, MaterialLibrary.Ebony, new Vector3(s * W * 0.2f, top * 0.97f, bridgeZ), new Vector3(W * 0.025f, 0.002f, L * 0.2f), Quaternion.Euler(0, s * 8f, 0));

            // Fingerboard, neck, pegbox, scroll
            float fbLen = nutZ - fbStart;
            Part("Fingerboard", root, PrimitiveType.Cube, MaterialLibrary.Ebony, new Vector3(0, top + bridgeH * 0.45f, fbStart + fbLen * 0.5f), new Vector3(W * 0.2f, L * 0.02f, fbLen), Quaternion.Euler(-4f, 0, 0));
            Part("Neck", root, PrimitiveType.Cube, MaterialLibrary.Maple, new Vector3(0, top * 0.5f, (neckBodyZ + nutZ) * 0.5f), new Vector3(W * 0.14f, D * 0.45f, nutZ - neckBodyZ));
            Part("Pegbox", root, PrimitiveType.Cube, varnish, new Vector3(0, top * 0.4f, (nutZ + scrollZ) * 0.5f), new Vector3(W * 0.13f, D * 0.5f, scrollZ - nutZ));
            Part("Scroll", root, PrimitiveType.Sphere, varnish, new Vector3(0, top * 0.5f, scrollZ + L * 0.03f), new Vector3(W * 0.16f, D * 0.75f, L * 0.1f));
            for (int p = 0; p < 4; p++)
            {
                float pz = nutZ + (scrollZ - nutZ) * (0.2f + p * 0.2f);
                Part("Peg", root, PrimitiveType.Cylinder, MaterialLibrary.Ebony, new Vector3((p % 2 == 0 ? 1 : -1) * W * 0.1f, top * 0.4f, pz), new Vector3(L * 0.02f, W * 0.08f, L * 0.02f), Quaternion.Euler(0, 0, 90));
            }

            // Bridge, tailpiece, strings
            Part("Bridge", root, PrimitiveType.Cube, MaterialLibrary.Maple, new Vector3(0, top + bridgeH * 0.5f, bridgeZ), new Vector3(W * 0.32f, bridgeH, 0.006f));
            float tailStart = e + L * 0.08f, tailEnd = e + L * 0.38f;
            Part("Tailpiece", root, PrimitiveType.Cube, MaterialLibrary.Ebony, new Vector3(0, top + bridgeH * 0.2f, (tailStart + tailEnd) * 0.5f), new Vector3(W * 0.22f, 0.012f, tailEnd - tailStart));
            for (int s = 0; s < 4; s++)
            {
                float x = (s - 1.5f) * W * 0.045f;
                Vector3 a = new Vector3(x, top + bridgeH * 0.3f, tailEnd);
                Vector3 b = new Vector3(x * 0.7f, top + bridgeH * 0.62f, nutZ);
                Vector3 mid = (a + b) * 0.5f;
                Part("String", root, PrimitiveType.Cylinder, MaterialLibrary.StringMetal, mid,
                    new Vector3(0.0018f + s * 0.0006f, (b - a).magnitude * 0.5f, 0.0018f + s * 0.0006f), Quaternion.FromToRotation(Vector3.up, b - a));
            }

            bowContactLocal = new Vector3(0, top + bridgeH * 0.93f, e + L * 0.6f);
            return root;
        }

        /// <summary>Bow: origin at the frog, +Z toward the tip.</summary>
        public static Transform BuildBow(InstrumentDefinition d, Transform parent)
        {
            var root = new GameObject("Bow").transform;
            root.SetParent(parent, false);
            float len = d.BowLength;
            Part("Stick", root, PrimitiveType.Cylinder, MaterialLibrary.BowStick, new Vector3(0, 0.014f, len * 0.5f), new Vector3(0.008f, len * 0.5f, 0.008f), Quaternion.Euler(90, 0, 0));
            Part("Hair", root, PrimitiveType.Cube, MaterialLibrary.BowHair, new Vector3(0, 0f, len * 0.5f), new Vector3(0.009f, 0.002f, len * 0.96f));
            Part("Frog", root, PrimitiveType.Cube, MaterialLibrary.Ebony, new Vector3(0, 0.008f, 0.02f), new Vector3(0.014f, 0.022f, 0.045f));
            Part("Tip", root, PrimitiveType.Cube, MaterialLibrary.BowHair, new Vector3(0, 0.01f, len), new Vector3(0.009f, 0.02f, 0.012f));
            return root;
        }

        public static Transform BuildChair(Transform parent, Vector3 localPos, float yaw)
        {
            var root = Empty("Chair", parent, localPos);
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            Part("Seat", root, PrimitiveType.Cube, MaterialLibrary.ChairLeather, new Vector3(0, 0.45f, 0), new Vector3(0.46f, 0.06f, 0.44f));
            Part("Back", root, PrimitiveType.Cube, MaterialLibrary.ChairLeather, new Vector3(0, 0.78f, -0.21f), new Vector3(0.44f, 0.4f, 0.04f), Quaternion.Euler(-8, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * 0.2f, z = (i < 2 ? -1 : 1) * 0.19f;
                Part("Leg", root, PrimitiveType.Cylinder, MaterialLibrary.SteelBlack, new Vector3(x, 0.21f, z), new Vector3(0.025f, 0.21f, 0.025f));
            }
            return root;
        }

        public static Transform BuildStool(Transform parent, Vector3 localPos)
        {
            var root = Empty("Bass Stool", parent, localPos);
            Part("Seat", root, PrimitiveType.Cylinder, MaterialLibrary.ChairLeather, new Vector3(0, 0.74f, 0), new Vector3(0.36f, 0.03f, 0.36f));
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                Vector3 foot = new Vector3(Mathf.Sin(a) * 0.22f, 0f, Mathf.Cos(a) * 0.22f);
                Vector3 topP = new Vector3(Mathf.Sin(a) * 0.1f, 0.73f, Mathf.Cos(a) * 0.1f);
                Part("Leg", root, PrimitiveType.Cylinder, MaterialLibrary.SteelBlack, (foot + topP) * 0.5f, new Vector3(0.022f, (topP - foot).magnitude * 0.5f, 0.022f), Quaternion.FromToRotation(Vector3.up, topP - foot));
            }
            return root;
        }

        public static Transform BuildMusicStand(Transform parent, Vector3 localPos, float yaw, float height)
        {
            var root = Empty("Music Stand", parent, localPos);
            root.localRotation = Quaternion.Euler(0, yaw, 0);
            Part("Base", root, PrimitiveType.Cylinder, MaterialLibrary.SteelBlack, new Vector3(0, 0.01f, 0), new Vector3(0.4f, 0.01f, 0.4f));
            Part("Pole", root, PrimitiveType.Cylinder, MaterialLibrary.SteelBlack, new Vector3(0, height * 0.5f, 0), new Vector3(0.025f, height * 0.5f, 0.025f));
            var desk = Empty("Desk", root, new Vector3(0, height + 0.12f, 0));
            desk.localRotation = Quaternion.Euler(-28f, 180f, 0);
            Part("Plate", desk, PrimitiveType.Cube, MaterialLibrary.SteelBlack, Vector3.zero, new Vector3(0.52f, 0.36f, 0.012f));
            Part("Ledge", desk, PrimitiveType.Cube, MaterialLibrary.SteelBlack, new Vector3(0, -0.18f, 0.03f), new Vector3(0.52f, 0.015f, 0.06f));
            Part("Sheet L", desk, PrimitiveType.Cube, MaterialLibrary.SheetMusic, new Vector3(-0.12f, 0.01f, 0.008f), new Vector3(0.23f, 0.31f, 0.002f));
            Part("Sheet R", desk, PrimitiveType.Cube, MaterialLibrary.SheetMusic, new Vector3(0.12f, 0.01f, 0.008f), new Vector3(0.23f, 0.31f, 0.002f));
            return root;
        }
    }
}
