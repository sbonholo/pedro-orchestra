using System.Collections.Generic;
using UnityEngine;

namespace OrchestraProject
{
    /// <summary>
    /// Creates all materials at runtime from the render pipeline's default Lit shader
    /// (URP Lit in a Universal 3D project). Textures are generated procedurally.
    /// PLACEHOLDER: replace with authored PBR materials (albedo + normal + roughness maps) in Stage 3.
    /// </summary>
    public static class MaterialLibrary
    {
        static Shader litShader;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Shader Lit
        {
            get
            {
                if (litShader == null)
                {
                    var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    litShader = probe.GetComponent<Renderer>().sharedMaterial.shader;
                    Object.Destroy(probe);
                }
                return litShader;
            }
        }

        public static Material Create(string key, Color color, float smoothness = 0.3f, float metallic = 0f,
            Texture2D texture = null, Vector2? tiling = null)
        {
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Lit) { name = key };
            SetColor(m, color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (texture != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
                if (tiling.HasValue)
                {
                    if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling.Value);
                    if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling.Value);
                }
            }
            m.enableInstancing = true;
            cache[key] = m;
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static Material Get(string key) { Material m; cache.TryGetValue(key, out m); return m; }

        // ---- Named materials used across the game ----
        public static Material StageFloor { get { return Create("StageFloor", new Color(0.66f, 0.46f, 0.29f), 0.5f, 0f, ProceduralTextures.WoodPlanks, new Vector2(8f, 5f)); } }
        public static Material WarmWood { get { return Create("WarmWood", new Color(0.62f, 0.40f, 0.24f), 0.45f, 0f, ProceduralTextures.WoodGrain, new Vector2(1f, 2f)); } }
        public static Material DarkWood { get { return Create("DarkWood", new Color(0.30f, 0.18f, 0.11f), 0.45f, 0f, ProceduralTextures.WoodGrain, new Vector2(1f, 1f)); } }
        public static Material Velvet { get { return Create("Velvet", new Color(0.50f, 0.07f, 0.09f), 0.15f, 0f, ProceduralTextures.Fabric, new Vector2(2f, 2f)); } }
        public static Material Carpet { get { return Create("Carpet", new Color(0.30f, 0.05f, 0.07f), 0.05f, 0f, ProceduralTextures.Fabric, new Vector2(6f, 20f)); } }
        public static Material Plaster { get { return Create("Plaster", new Color(0.83f, 0.78f, 0.69f), 0.12f); } }
        public static Material PlasterShade { get { return Create("PlasterShade", new Color(0.68f, 0.62f, 0.53f), 0.15f); } }
        public static Material Gold { get { return Create("Gold", new Color(0.86f, 0.68f, 0.36f), 0.75f, 0.9f); } }
        public static Material OrganPipe { get { return Create("OrganPipe", new Color(0.82f, 0.80f, 0.76f), 0.8f, 1f); } }
        public static Material Ceiling { get { return Create("Ceiling", new Color(0.35f, 0.24f, 0.17f), 0.3f, 0f, ProceduralTextures.WoodGrain, new Vector2(6f, 6f)); } }
        public static Material WarmLamp { get { return Create("WarmLamp", new Color(1.0f, 0.88f, 0.62f), 0.9f); } }
        public static Material BlackFabric { get { return Create("BlackFabric", new Color(0.07f, 0.07f, 0.08f), 0.22f, 0f, ProceduralTextures.Fabric, new Vector2(1f, 1f)); } }
        public static Material WhiteShirt { get { return Create("WhiteShirt", new Color(0.88f, 0.87f, 0.84f), 0.25f); } }
        public static Material ChairLeather { get { return Create("ChairLeather", new Color(0.08f, 0.07f, 0.07f), 0.4f); } }
        public static Material SteelBlack { get { return Create("SteelBlack", new Color(0.16f, 0.16f, 0.18f), 0.68f, 0.85f); } }
        public static Material Varnish { get { return Create("Varnish", new Color(0.62f, 0.27f, 0.10f), 0.85f, 0f, ProceduralTextures.WoodGrain, new Vector2(1f, 1f)); } }
        public static Material VarnishDark { get { return Create("VarnishDark", new Color(0.42f, 0.17f, 0.07f), 0.85f, 0f, ProceduralTextures.WoodGrain, new Vector2(1f, 1f)); } }
        public static Material Ebony { get { return Create("Ebony", new Color(0.03f, 0.025f, 0.02f), 0.7f); } }
        public static Material Maple { get { return Create("Maple", new Color(0.86f, 0.74f, 0.55f), 0.4f); } }
        public static Material StringMetal { get { return Create("StringMetal", new Color(0.78f, 0.76f, 0.72f), 0.85f, 1f); } }
        public static Material BowHair { get { return Create("BowHair", new Color(0.93f, 0.91f, 0.85f), 0.3f); } }
        public static Material BowStick { get { return Create("BowStick", new Color(0.30f, 0.13f, 0.06f), 0.7f); } }
        public static Material SheetMusic { get { return Create("SheetMusic", new Color(0.97f, 0.95f, 0.90f), 0.1f, 0f, ProceduralTextures.SheetMusic, new Vector2(1f, 1f)); } }
        public static Material SelectionRing { get { return Decal("SelectionHalo", ProceduralTextures.Halo, new Color(1f, 0.78f, 0.4f, 1f)); } }
        /// <summary>Soft dark blob under figures and chairs so they sit on the floor (same as the web preview's contact shadows).</summary>
        public static Material ContactShadow { get { return Decal("ContactShadow", ProceduralTextures.Blob, Color.white); } }

        /// <summary>Unlit, alpha-blended material for floor decals (Sprites/Default renders in URP's unlit pass).</summary>
        public static Material Decal(string key, Texture2D tex, Color tint)
        {
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) return Create(key, tint, 0.9f);
            m = new Material(sh) { name = key, mainTexture = tex, color = tint, renderQueue = 3000 };
            cache[key] = m;
            return m;
        }

        public static Material Skin(float tone)
        {
            string key = "Skin" + Mathf.RoundToInt(tone * 10);
            Color light = new Color(0.93f, 0.76f, 0.64f), dark = new Color(0.36f, 0.22f, 0.14f);
            return Create(key, Color.Lerp(light, dark, Mathf.Round(tone * 10) / 10f), 0.35f);
        }

        public static Material Jacket { get { return Create("Jacket", new Color(0.065f, 0.065f, 0.078f), 0.28f, 0f, ProceduralTextures.Fabric, new Vector2(1f, 1f)); } }
        public static Material Satin { get { return Create("Satin", new Color(0.024f, 0.024f, 0.028f), 0.78f); } }
        public static Material Shoe { get { return Create("Shoe", new Color(0.027f, 0.027f, 0.027f), 0.82f, 0.2f); } }
        public static Material EyeWhite { get { return Create("EyeWhite", new Color(0.94f, 0.91f, 0.88f), 0.7f); } }
        public static Material Iris { get { return Create("Iris", new Color(0.14f, 0.09f, 0.06f), 0.85f); } }
        public static Material Pearl { get { return Create("Pearl", new Color(0.95f, 0.93f, 0.89f), 0.75f, 0.1f); } }

        public static Material Lip(float tone)
        {
            string key = "Lip" + Mathf.RoundToInt(tone * 10);
            Color light = new Color(0.79f, 0.54f, 0.49f), dark = new Color(0.29f, 0.16f, 0.13f);
            return Create(key, Color.Lerp(light, dark, Mathf.Round(tone * 10) / 10f), 0.55f);
        }

        public static Material Hair(int variant)
        {
            Color[] colors = { new Color(0.05f, 0.04f, 0.035f), new Color(0.22f, 0.13f, 0.07f), new Color(0.48f, 0.33f, 0.18f), new Color(0.65f, 0.55f, 0.40f), new Color(0.55f, 0.55f, 0.55f) };
            variant = Mathf.Abs(variant) % colors.Length;
            return Create("Hair" + variant, colors[variant], 0.35f);
        }
    }

    /// <summary>Small procedural textures so surfaces are not flat colour. PLACEHOLDER for real texture sets.</summary>
    public static class ProceduralTextures
    {
        static Texture2D woodPlanks, woodGrain, fabric, sheet, halo, blob;

        /// <summary>Radial alpha gradient: a soft ring (halo) or a soft dark spot (blob).</summary>
        static Texture2D Radial(bool ring)
        {
            const int size = 128;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).magnitude / (size / 2f);
                    float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(r - 0.8f) / 0.14f) : 0.55f * Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r);
                    px[y * size + x] = ring ? new Color(1f, 1f, 1f, a * a) : new Color(0f, 0f, 0f, a);
                }
            t.SetPixels(px); t.Apply(true);
            return t;
        }
        public static Texture2D Halo { get { return halo != null ? halo : (halo = Radial(true)); } }
        public static Texture2D Blob { get { return blob != null ? blob : (blob = Radial(false)); } }

        static Texture2D NewTex(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            return t;
        }

        public static Texture2D WoodPlanks
        {
            get
            {
                if (woodPlanks != null) return woodPlanks;
                const int size = 512;
                woodPlanks = NewTex(size);
                var px = new Color[size * size];
                int planks = 8;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        int plank = y * planks / size;
                        float offset = plank * 37.13f;
                        float grain = Mathf.PerlinNoise((x + offset * 13f) * 0.01f, y * 0.25f + offset);
                        float rings = Mathf.Sin((grain * 12f + y * 0.05f)) * 0.5f + 0.5f;
                        float plankTint = 0.86f + 0.14f * Mathf.PerlinNoise(plank * 3.3f, 0.5f);
                        float v = (0.78f + 0.22f * rings) * plankTint;
                        bool seam = (y % (size / planks)) < 2 || ((x + plank * 97) % size) < 2;
                        if (seam) v *= 0.55f;
                        px[y * size + x] = new Color(v, v, v, 1f);
                    }
                woodPlanks.SetPixels(px); woodPlanks.Apply(true);
                return woodPlanks;
            }
        }

        public static Texture2D WoodGrain
        {
            get
            {
                if (woodGrain != null) return woodGrain;
                const int size = 256;
                woodGrain = NewTex(size);
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.02f, y * 0.004f);
                        float rings = Mathf.Sin(n * 40f) * 0.5f + 0.5f;
                        float v = 0.82f + 0.18f * rings;
                        px[y * size + x] = new Color(v, v, v, 1f);
                    }
                woodGrain.SetPixels(px); woodGrain.Apply(true);
                return woodGrain;
            }
        }

        public static Texture2D Fabric
        {
            get
            {
                if (fabric != null) return fabric;
                const int size = 128;
                fabric = NewTex(size);
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.12f + Mathf.PerlinNoise(x * 0.6f, y * 0.6f) * 0.06f;
                        float weave = ((x + y) % 2 == 0) ? 0.02f : 0f;
                        float v = 0.85f + n + weave;
                        px[y * size + x] = new Color(v, v, v, 1f);
                    }
                fabric.SetPixels(px); fabric.Apply(true);
                return fabric;
            }
        }

        /// <summary>A page of (decorative) staff lines with note heads for music stands.</summary>
        public static Texture2D SheetMusic
        {
            get
            {
                if (sheet != null) return sheet;
                const int w = 256, h = 256;
                sheet = NewTex(w);
                var px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                var rng = new System.Random(3);
                for (int staff = 0; staff < 7; staff++)
                {
                    int top = h - 28 - staff * 32;
                    for (int line = 0; line < 5; line++)
                    {
                        int y = top - line * 4;
                        for (int x = 14; x < w - 14; x++) px[y * w + x] = new Color(0.25f, 0.25f, 0.25f);
                    }
                    for (int x = 26; x < w - 20; x += 12 + rng.Next(8))
                    {
                        int ny = top - rng.Next(0, 17);
                        for (int dy = -2; dy <= 2; dy++)
                            for (int dx = -3; dx <= 3; dx++)
                                if (dx * dx + dy * dy * 2 <= 9 && ny + dy > 0 && ny + dy < h) px[(ny + dy) * w + x + dx] = new Color(0.1f, 0.1f, 0.1f);
                        for (int sy = 0; sy < 12; sy++) if (ny + sy < h) px[(ny + sy) * w + x + 3] = new Color(0.15f, 0.15f, 0.15f);
                    }
                }
                sheet.SetPixels(px); sheet.Apply(true);
                return sheet;
            }
        }
    }
}
