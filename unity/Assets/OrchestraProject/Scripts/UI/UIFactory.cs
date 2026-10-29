using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrchestraProject
{
    /// <summary>Colours, font and generated sprites for a dark, concert-hall style interface.</summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color(0.055f, 0.052f, 0.065f, 0.93f);
        public static readonly Color Panel = new Color(0.10f, 0.095f, 0.115f, 0.96f);
        public static readonly Color PanelLight = new Color(0.15f, 0.14f, 0.165f, 1f);
        public static readonly Color Accent = new Color(0.86f, 0.69f, 0.42f);
        public static readonly Color AccentDim = new Color(0.47f, 0.37f, 0.22f);
        public static readonly Color Text = new Color(0.94f, 0.92f, 0.88f);
        public static readonly Color TextDim = new Color(0.66f, 0.64f, 0.60f);
        public static readonly Color Good = new Color(0.47f, 0.78f, 0.52f);
        public static readonly Color Bad = new Color(0.90f, 0.43f, 0.37f);
        public static readonly Color Button = new Color(0.19f, 0.18f, 0.21f);
        public static readonly Color ButtonHover = new Color(0.27f, 0.25f, 0.29f);
        public static readonly Color ButtonPressed = new Color(0.36f, 0.30f, 0.22f);
        public static readonly Color BarBack = new Color(0.06f, 0.06f, 0.07f, 1f);

        static Font font;
        static Sprite rounded, circle;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>9-sliced rounded rectangle (generated, no asset files needed).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) rounded = MakeSprite(64, 14f, 18);
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null) circle = MakeSprite(128, 63f, 0);
                return circle;
            }
        }

        static Sprite MakeSprite(int size, float radius, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        public static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
    }

    /// <summary>Helpers that build uGUI elements in code, so no prefabs or scene setup are needed.</summary>
    public static class UIFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            return Anchor(rt, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded) { img.sprite = UITheme.Rounded; img.type = Image.Type.Sliced; }
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 22, TextAnchor align = TextAnchor.MiddleLeft,
            Color? color = null, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UITheme.Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? UITheme.Text;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, int size = 22, float height = 64f, Color? baseColor = null)
        {
            var img = Panel(parent, "Button " + label, Color.white);
            var b = img.gameObject.AddComponent<Button>();
            SetColors(b, baseColor ?? UITheme.Button);
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Label(img.transform, label, size, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 10, 4, 10, 4);
            Size(img, height);
            return b;
        }

        public static void SetColors(Button b, Color normal)
        {
            var c = b.colors;
            c.normalColor = normal;
            c.highlightedColor = Color.Lerp(normal, Color.white, 0.12f);
            c.selectedColor = normal;
            c.pressedColor = UITheme.ButtonPressed;
            c.disabledColor = new Color(normal.r * 0.6f, normal.g * 0.6f, normal.b * 0.6f, 0.6f);
            c.fadeDuration = 0.08f;
            b.colors = c;
        }

        public static void SetSelected(Button b, bool on)
        {
            SetColors(b, on ? UITheme.AccentDim : UITheme.Button);
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.color = on ? Color.white : UITheme.Text;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static LayoutElement Size(Component c, float prefHeight = -1, float prefWidth = -1, float flexWidth = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            // Text keeps its natural (wrapped) height and only gets a minimum, so long text never overlaps.
            if (prefHeight >= 0) { le.minHeight = prefHeight; if (!(c is Text)) le.preferredHeight = prefHeight; }
            if (prefWidth >= 0) { le.preferredWidth = prefWidth; le.minWidth = Mathf.Min(prefWidth, 60f); }
            if (flexWidth >= 0) le.flexibleWidth = flexWidth;
            return le;
        }

        public static VerticalLayoutGroup Vertical(Component c, float spacing = 8, int padding = 0, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = c.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup Horizontal(Component c, float spacing = 8, int padding = 0, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(padding, padding, padding, padding);
            h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = false;
            return h;
        }

        /// <summary>A row container (fixed height) for placing items side by side.</summary>
        public static RectTransform Row(Transform parent, float height, float spacing = 8)
        {
            var rt = Rect("Row", parent);
            Horizontal(rt, spacing);
            Size(rt, height);
            return rt;
        }

        /// <summary>Vertical scrolling list. Returns the content transform to add children to.</summary>
        public static RectTransform ScrollArea(Transform parent, out ScrollRect scroll)
        {
            var area = Rect("Scroll Area", parent);
            var hit = area.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0.001f);
            scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;

            var viewport = Stretch(Rect("Viewport", area));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            Vertical(content, 10, 4);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        /// <summary>Horizontal value bar. Returns the fill rect; update with <see cref="SetBar"/>.</summary>
        public static RectTransform Bar(Transform parent, float height, Color fill, float value01)
        {
            var back = Panel(parent, "Bar", UITheme.BarBack);
            Size(back, height, -1, 1);
            var f = Panel(back.transform, "Fill", fill);
            var rt = f.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static void SetBar(RectTransform fill, float value01)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }

        /// <summary>Centred dialog panel: fixed width, full height minus a margin (fits every screen).</summary>
        public static Image Dialog(Transform parent, string name, float width)
        {
            var p = Panel(parent, name, UITheme.Panel);
            float half = Mathf.Min(width * 0.5f, 900f);
            Anchor(p.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-half, 14), new Vector2(half, -14));
            return p;
        }

        public static Text Header(Transform parent, string text)
        {
            var t = Label(parent, text.ToUpperInvariant(), 18, TextAnchor.LowerLeft, UITheme.Accent, FontStyle.Bold);
            Size(t, 34);
            return t;
        }

        public static Color SkillColor(float value)
        {
            if (value >= 80f) return new Color(0.95f, 0.78f, 0.40f);
            if (value >= 65f) return new Color(0.55f, 0.80f, 0.62f);
            if (value >= 50f) return new Color(0.50f, 0.70f, 0.88f);
            if (value >= 35f) return new Color(0.62f, 0.62f, 0.75f);
            return new Color(0.62f, 0.52f, 0.52f);
        }
    }

    /// <summary>Fires on pointer DOWN (not click) so rhythm taps are measured at the moment of touch.</summary>
    public class TapPad : MonoBehaviour, IPointerDownHandler
    {
        public Action OnTap;
        public void OnPointerDown(PointerEventData eventData) { if (OnTap != null) OnTap(); }
    }

    /// <summary>Keeps UI inside the iPhone notch / home-indicator safe area.</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect applied;
        Vector2Int screen;

        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == applied && screen.x == Screen.width && screen.y == Screen.height) return;
            applied = sa; screen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
