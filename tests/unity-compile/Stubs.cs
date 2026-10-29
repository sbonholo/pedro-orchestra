// Minimal API stubs (signatures mirror the real packages) for compile-checking only.
using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class BaseEventData { }
    public class PointerEventData : BaseEventData { }
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData eventData); }
    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get; set; }
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
    }
    public abstract class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
}

namespace UnityEngine.UI
{
    using UnityEngine.EventSystems;
    public interface ILayoutElement { }
    public abstract class Graphic : UIBehaviour
    {
        public virtual Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform => null;
    }
    public abstract class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic, ILayoutElement
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public Sprite sprite { get; set; }
        public Type type { get; set; }
    }
    public class Text : MaskableGraphic, ILayoutElement
    {
        public Font font { get; set; }
        public string text { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public FontStyle fontStyle { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
    }
    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }
    }
    public class Selectable : UIBehaviour
    {
        public bool interactable { get; set; }
        public ColorBlock colors { get; set; }
    }
    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; }
    }
    public class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public MovementType movementType { get; set; }
        public float scrollSensitivity { get; set; }
        public bool inertia { get; set; }
        public RectTransform viewport { get; set; }
        public RectTransform content { get; set; }
    }
    public class LayoutElement : UIBehaviour, ILayoutElement
    {
        public bool ignoreLayout { get; set; }
        public float minWidth { get; set; }
        public float minHeight { get; set; }
        public float preferredWidth { get; set; }
        public float preferredHeight { get; set; }
        public float flexibleWidth { get; set; }
        public float flexibleHeight { get; set; }
    }
    public abstract class LayoutGroup : UIBehaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class ContentSizeFitter : UIBehaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode verticalFit { get; set; }
        public FitMode horizontalFit { get; set; }
    }
    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
    }
    public class GraphicRaycaster : MonoBehaviour { }
    public class RectMask2D : UIBehaviour { }
}

#if ENABLE_INPUT_SYSTEM
namespace UnityEngine.InputSystem.Utilities
{
    public struct ReadOnlyArray<T> : System.Collections.Generic.IEnumerable<T>
    {
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() => null;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null;
        public int Count => 0;
    }
}
namespace UnityEngine.InputSystem.Controls
{
    public class InputControl<T> where T : struct { public T ReadValue() => default; }
    public class ButtonControl : InputControl<float> { public bool isPressed => false; public bool wasPressedThisFrame => false; public bool wasReleasedThisFrame => false; }
    public class KeyControl : ButtonControl { }
    public class Vector2Control : InputControl<Vector2> { }
    public class DeltaControl : Vector2Control { }
    public class IntegerControl : InputControl<int> { }
    public class TouchPressControl : ButtonControl { }
    public class TouchControl
    {
        public TouchPressControl press => null;
        public IntegerControl touchId => null;
        public Vector2Control position => null;
        public DeltaControl delta => null;
    }
}
namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;
    using UnityEngine.InputSystem.Utilities;
    public enum Key { None, Space, Enter, Escape, A, D, E, F, Q, S, W, UpArrow, DownArrow, LeftArrow, RightArrow, LeftShift, Digit1, Digit2, Digit3, Digit4, Digit5 }
    public class Keyboard { public static Keyboard current => null; public KeyControl this[Key key] => null; }
    public class Mouse
    {
        public static Mouse current => null;
        public Vector2Control position => null;
        public DeltaControl delta => null;
        public DeltaControl scroll => null;
        public ButtonControl leftButton => null; public ButtonControl rightButton => null; public ButtonControl middleButton => null;
    }
    public class Touchscreen { public static Touchscreen current => null; public ReadOnlyArray<TouchControl> touches => default; }
}
namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { public void AssignDefaultActions() { } }
}
#endif

namespace UnityEngine.Rendering
{
    public class VolumeParameter<T> { public virtual void Override(T x) { } }
    public class ClampedFloatParameter : VolumeParameter<float> { }
    public class MinFloatParameter : VolumeParameter<float> { }
    public class FloatParameter : VolumeParameter<float> { }
    public class VolumeComponent : ScriptableObject { }
    public class VolumeProfile : ScriptableObject { public T Add<T>(bool overrides = false) where T : VolumeComponent => null; }
    public class Volume : MonoBehaviour { public bool isGlobal { get; set; } public VolumeProfile sharedProfile { get; set; } }
}
namespace UnityEngine.Rendering.Universal
{
    using UnityEngine.Rendering;
    public enum TonemappingMode { None, Neutral, ACES }
    public class TonemappingModeParameter : VolumeParameter<TonemappingMode> { }
    public class Tonemapping : VolumeComponent { public TonemappingModeParameter mode; }
    public class Bloom : VolumeComponent { public MinFloatParameter threshold, intensity; public ClampedFloatParameter scatter; }
    public class Vignette : VolumeComponent { public ClampedFloatParameter intensity, smoothness; }
    public class ColorAdjustments : VolumeComponent { public FloatParameter postExposure; public ClampedFloatParameter contrast, saturation; }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public class UniversalAdditionalCameraData : MonoBehaviour { public bool renderPostProcessing { get; set; } public AntialiasingMode antialiasing { get; set; } }
    public static class CameraExtensions { public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera c) => null; }
    public class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public float shadowDistance { get; set; }
        public override RenderPipeline CreatePipeline() => null;
    }
}

namespace UnityEditor
{
    public sealed class MenuItem : Attribute { public MenuItem(string itemName) { } public int priority; }
    public static class AssetDatabase { public static bool IsValidFolder(string p) => true; public static string CreateFolder(string a, string b) => ""; }
    public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string path, bool enabled) { } public string path => ""; }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get; set; } }
    public enum UIOrientation { Portrait, PortraitUpsideDown, LandscapeRight, LandscapeLeft, AutoRotation }
    public static class PlayerSettings
    {
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; }
        public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; }
        public static bool allowedAutorotateToLandscapeRight { get; set; }
    }
    public static class EditorUtility
    {
        public static bool DisplayDialog(string t, string m, string ok) => true;
        public static bool DisplayDialog(string t, string m, string ok, string cancel) => true;
        public static void RevealInFinder(string p) { }
    }
}
namespace UnityEditor
{
    public sealed class InitializeOnLoadAttribute : Attribute { }
    public static class EditorApplication
    {
        public delegate void CallbackFunction();
        public static CallbackFunction delayCall;
        public static bool isPlayingOrWillChangePlaymode => false;
    }
    public static class SessionState
    {
        public static bool GetBool(string key, bool defaultValue) => defaultValue;
        public static void SetBool(string key, bool value) { }
    }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m) => default;
        public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string path) => true;
        public static UnityEngine.SceneManagement.Scene OpenScene(string path) => default;
    }
}
