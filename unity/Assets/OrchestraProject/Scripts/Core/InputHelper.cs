using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OrchestraProject
{
    public enum GameKey { Space, Enter, Escape, W, A, S, D, Q, E, Up, Down, Left, Right, Shift, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, F }

    public struct TouchPoint
    {
        public int Id;
        public Vector2 Position;
        public Vector2 Delta;
        public bool Began;
    }

    /// <summary>
    /// One place for all input, so the game works whether the project uses Unity's new
    /// Input System package (Unity 6 default) or the old Input Manager.
    /// </summary>
    public static class InputHelper
    {
#if ENABLE_INPUT_SYSTEM
        static Key Map(GameKey k)
        {
            switch (k)
            {
                case GameKey.Space: return Key.Space;
                case GameKey.Enter: return Key.Enter;
                case GameKey.Escape: return Key.Escape;
                case GameKey.W: return Key.W;
                case GameKey.A: return Key.A;
                case GameKey.S: return Key.S;
                case GameKey.D: return Key.D;
                case GameKey.Q: return Key.Q;
                case GameKey.E: return Key.E;
                case GameKey.Up: return Key.UpArrow;
                case GameKey.Down: return Key.DownArrow;
                case GameKey.Left: return Key.LeftArrow;
                case GameKey.Right: return Key.RightArrow;
                case GameKey.Shift: return Key.LeftShift;
                case GameKey.Alpha1: return Key.Digit1;
                case GameKey.Alpha2: return Key.Digit2;
                case GameKey.Alpha3: return Key.Digit3;
                case GameKey.Alpha4: return Key.Digit4;
                case GameKey.Alpha5: return Key.Digit5;
                default: return Key.F;
            }
        }

        public static bool KeyDown(GameKey k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].wasPressedThisFrame; }
        public static bool KeyHeld(GameKey k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].isPressed; }
        public static bool HasMouse { get { return Mouse.current != null; } }
        public static Vector2 MousePosition { get { return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero; } }
        public static Vector2 MouseDelta { get { return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero; } }
        public static float Scroll { get { return Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f; } }
        public static bool MouseHeld(int button)
        {
            var m = Mouse.current; if (m == null) return false;
            return button == 0 ? m.leftButton.isPressed : button == 1 ? m.rightButton.isPressed : m.middleButton.isPressed;
        }
        public static bool MouseDown(int button)
        {
            var m = Mouse.current; if (m == null) return false;
            return button == 0 ? m.leftButton.wasPressedThisFrame : button == 1 ? m.rightButton.wasPressedThisFrame : m.middleButton.wasPressedThisFrame;
        }
        public static bool MouseUp(int button)
        {
            var m = Mouse.current; if (m == null) return false;
            return button == 0 ? m.leftButton.wasReleasedThisFrame : button == 1 ? m.rightButton.wasReleasedThisFrame : m.middleButton.wasReleasedThisFrame;
        }

        static readonly System.Collections.Generic.List<TouchPoint> touchBuffer = new System.Collections.Generic.List<TouchPoint>();
        public static System.Collections.Generic.List<TouchPoint> Touches
        {
            get
            {
                touchBuffer.Clear();
                var ts = Touchscreen.current;
                if (ts == null) return touchBuffer;
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    touchBuffer.Add(new TouchPoint
                    {
                        Id = t.touchId.ReadValue(),
                        Position = t.position.ReadValue(),
                        Delta = t.delta.ReadValue(),
                        Began = t.press.wasPressedThisFrame
                    });
                }
                return touchBuffer;
            }
        }
#else
        static KeyCode Map(GameKey k)
        {
            switch (k)
            {
                case GameKey.Space: return KeyCode.Space;
                case GameKey.Enter: return KeyCode.Return;
                case GameKey.Escape: return KeyCode.Escape;
                case GameKey.W: return KeyCode.W;
                case GameKey.A: return KeyCode.A;
                case GameKey.S: return KeyCode.S;
                case GameKey.D: return KeyCode.D;
                case GameKey.Q: return KeyCode.Q;
                case GameKey.E: return KeyCode.E;
                case GameKey.Up: return KeyCode.UpArrow;
                case GameKey.Down: return KeyCode.DownArrow;
                case GameKey.Left: return KeyCode.LeftArrow;
                case GameKey.Right: return KeyCode.RightArrow;
                case GameKey.Shift: return KeyCode.LeftShift;
                case GameKey.Alpha1: return KeyCode.Alpha1;
                case GameKey.Alpha2: return KeyCode.Alpha2;
                case GameKey.Alpha3: return KeyCode.Alpha3;
                case GameKey.Alpha4: return KeyCode.Alpha4;
                case GameKey.Alpha5: return KeyCode.Alpha5;
                default: return KeyCode.F;
            }
        }

        public static bool KeyDown(GameKey k) { return Input.GetKeyDown(Map(k)); }
        public static bool KeyHeld(GameKey k) { return Input.GetKey(Map(k)); }
        public static bool HasMouse { get { return Input.mousePresent; } }
        public static Vector2 MousePosition { get { return Input.mousePosition; } }
        public static Vector2 MouseDelta { get { return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f; } }
        public static float Scroll { get { return Input.mouseScrollDelta.y; } }
        public static bool MouseHeld(int button) { return Input.GetMouseButton(button); }
        public static bool MouseDown(int button) { return Input.GetMouseButtonDown(button); }
        public static bool MouseUp(int button) { return Input.GetMouseButtonUp(button); }

        static readonly System.Collections.Generic.List<TouchPoint> touchBuffer = new System.Collections.Generic.List<TouchPoint>();
        public static System.Collections.Generic.List<TouchPoint> Touches
        {
            get
            {
                touchBuffer.Clear();
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                    touchBuffer.Add(new TouchPoint { Id = t.fingerId, Position = t.position, Delta = t.deltaPosition, Began = t.phase == TouchPhase.Began });
                }
                return touchBuffer;
            }
        }
#endif
    }
}
