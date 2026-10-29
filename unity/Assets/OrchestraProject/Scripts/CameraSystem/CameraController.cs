using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OrchestraProject
{
    public enum CameraMode { Stage, Audience, Orchestra, CloseUp, Free, MenuOrbit }

    /// <summary>
    /// Preset cameras with smooth transitions, a close-up that follows the selected musician,
    /// and a free camera (mouse/keyboard on PC, drag/pinch on touch). Tapping a musician selects them.
    /// Keys: 1 Stage, 2 Audience, 3 Orchestra, 4 Close-up, 5/F Free.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public CameraMode Mode { get; private set; }
        public event Action<CameraMode> ModeChanged;
        public event Action<MusicianView> MusicianTapped;
        public Func<Transform> CloseUpTarget;   // provided by GameManager (selected musician)

        /// <summary>Screen fraction (0..1) not covered by HUD panels. Preset cameras centre the stage in it.</summary>
        [NonSerialized] public float FreeLeft = 0f, FreeRight = 1f;
        float viewShift, viewZoom = 1f;

        Camera cam;
        Vector3 targetPos;
        Quaternion targetRot;
        float targetFov = 50f;
        float yaw, pitch;
        Vector2 pressPos;
        bool pressOverUi, dragging, pressActive;
        float lastPinch;
        float orbitAngle;

        void Awake()
        {
            cam = GetComponent<Camera>();
            SetMode(CameraMode.MenuOrbit, true);
        }

        public void SetMode(CameraMode mode, bool instant = false)
        {
            Mode = mode;
            ComputePreset();
            if (instant) { transform.position = targetPos; transform.rotation = targetRot; cam.fieldOfView = targetFov; }
            var e = transform.eulerAngles; yaw = e.y; pitch = e.x > 180 ? e.x - 360 : e.x;
            if (ModeChanged != null) ModeChanged(mode);
        }

        void ComputePreset()
        {
            Vector3 stage = new Vector3(0, ConcertHallBuilder.StageHeight, 0);
            switch (Mode)
            {
                case CameraMode.Stage:
                    Pose(stage + new Vector3(0.35f, 2.35f, -0.45f), stage + new Vector3(0, 0.8f, 3.2f), 55f); break;   // over the conductor's shoulder
                case CameraMode.Audience:
                    Pose(new Vector3(-2.5f, 2.6f + 1.4f, -14f), stage + new Vector3(0, 1.0f, 3f), 38f); break;
                case CameraMode.Orchestra:
                    Pose(new Vector3(0f, 7.5f, -5.5f), stage + new Vector3(0, 0.4f, 3.3f), 50f); break;
                case CameraMode.CloseUp:
                    var t = CloseUpTarget != null ? CloseUpTarget() : null;
                    if (t != null)
                    {
                        var view = t.GetComponent<MusicianView>();
                        Vector3 focus = view != null ? view.FocusPoint : t.position + Vector3.up;
                        Vector3 offset = t.rotation * new Vector3(0.9f, 0.35f, 1.9f);
                        Pose(focus + offset, focus, 34f);
                    }
                    else Pose(stage + new Vector3(0, 2f, -1.5f), stage + new Vector3(0, 1f, 3f), 40f);
                    break;
                case CameraMode.MenuOrbit:
                    orbitAngle = 200f;
                    OrbitPose(0f); break;
            }
        }

        void Pose(Vector3 pos, Vector3 lookAt, float fov)
        {
            targetPos = pos;
            targetRot = Quaternion.LookRotation(lookAt - pos);
            targetFov = fov;
        }

        void OrbitPose(float dt)
        {
            orbitAngle += dt * 4f;
            float r = 11f, rad = orbitAngle * Mathf.Deg2Rad;
            Vector3 center = new Vector3(0, ConcertHallBuilder.StageHeight + 0.8f, 3f);
            Pose(center + new Vector3(Mathf.Sin(rad) * r, 3.2f, Mathf.Cos(rad) * r * 0.9f), center, 45f);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            HandleKeys();
            HandlePointer(dt);

            if (Mode == CameraMode.CloseUp) ComputePreset();       // follow the musician
            if (Mode == CameraMode.MenuOrbit) OrbitPose(dt);
            FrameFreeArea(1f - Mathf.Exp(-dt * 4.5f));
            if (Mode == CameraMode.Free)
            {
                transform.rotation = Quaternion.Euler(pitch, yaw, 0);
                FreeMove(dt);
                return;
            }
            float k = 1f - Mathf.Exp(-dt * (Mode == CameraMode.MenuOrbit ? 2f : 4.5f));
            transform.position = Vector3.Lerp(transform.position, targetPos, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, k);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, k);
        }

        // Same rule as the web preview: shift the image (lens shift) and widen it a little so no player hides behind a panel.
        void FrameFreeArea(float k)
        {
            float l = 0f, r = 1f;
            if (Mode != CameraMode.Free && Mode != CameraMode.MenuOrbit && FreeRight - FreeLeft >= 0.4f) { l = FreeLeft; r = FreeRight; }
            viewShift = Mathf.Lerp(viewShift, (l + r) * 0.5f - 0.5f, k);
            viewZoom = Mathf.Lerp(viewZoom, Mathf.Clamp((r - l) / 0.62f, 0.7f, 1f), k);
            if (Mathf.Abs(viewShift) < 0.001f && viewZoom > 0.999f) { cam.ResetProjectionMatrix(); return; }
            var p = Matrix4x4.Perspective(cam.fieldOfView, cam.aspect, cam.nearClipPlane, cam.farClipPlane);
            p.m00 *= viewZoom; p.m11 *= viewZoom;
            p.m02 = -2f * viewShift;
            cam.projectionMatrix = p;
        }

        void HandleKeys()
        {
            if (Mode == CameraMode.MenuOrbit) return;
            if (InputHelper.KeyDown(GameKey.Alpha1)) SetMode(CameraMode.Stage);
            if (InputHelper.KeyDown(GameKey.Alpha2)) SetMode(CameraMode.Audience);
            if (InputHelper.KeyDown(GameKey.Alpha3)) SetMode(CameraMode.Orchestra);
            if (InputHelper.KeyDown(GameKey.Alpha4)) SetMode(CameraMode.CloseUp);
            if (InputHelper.KeyDown(GameKey.Alpha5) || InputHelper.KeyDown(GameKey.F)) SetMode(CameraMode.Free);
        }

        static bool OverUi(int pointerId = -1)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return pointerId < 0 ? es.IsPointerOverGameObject() : es.IsPointerOverGameObject(pointerId) || es.IsPointerOverGameObject();
        }

        void HandlePointer(float dt)
        {
            if (Mode == CameraMode.MenuOrbit) return;
            var touches = InputHelper.Touches;

            if (touches.Count >= 2)
            {
                // Two fingers: pinch = move forward/back, drag = pan.
                if (touches[0].Began || touches[1].Began) { pressOverUi = OverUi(touches[0].Id); lastPinch = Vector2.Distance(touches[0].Position, touches[1].Position); }
                if (pressOverUi) return;
                EnterFree();
                float pinch = Vector2.Distance(touches[0].Position, touches[1].Position);
                transform.position += transform.forward * (pinch - lastPinch) * 0.01f;
                Vector2 pan = (touches[0].Delta + touches[1].Delta) * 0.5f;
                transform.position -= (transform.right * pan.x + transform.up * pan.y) * 0.004f;
                lastPinch = pinch;
                dragging = true;
                ClampPosition();
                return;
            }

            bool down, held, up;
            Vector2 pos, delta;
            if (touches.Count == 1)
            {
                var t = touches[0];
                down = t.Began; held = true; up = false; pos = t.Position; delta = t.Delta;
            }
            else if (InputHelper.HasMouse)
            {
                down = InputHelper.MouseDown(0) || InputHelper.MouseDown(1);
                held = InputHelper.MouseHeld(0) || InputHelper.MouseHeld(1);
                up = InputHelper.MouseUp(0) || InputHelper.MouseUp(1);
                pos = InputHelper.MousePosition; delta = InputHelper.MouseDelta;
                float scroll = InputHelper.Scroll;
                if (Mathf.Abs(scroll) > 0.01f && !OverUi())
                {
                    EnterFree();
                    transform.position += transform.forward * scroll * 0.8f;
                    ClampPosition();
                }
            }
            else { down = held = false; up = pressActive; pos = pressPos; delta = Vector2.zero; }

            if (down)
            {
                pressActive = true; dragging = false; pressPos = pos;
                pressOverUi = touches.Count == 1 ? OverUi(touches[0].Id) : OverUi();
            }
            if (pressActive && held && !pressOverUi)
            {
                if (!dragging && (pos - pressPos).magnitude > 12f) { dragging = true; EnterFree(); }
                if (dragging)
                {
                    yaw += delta.x * 0.15f;
                    pitch = Mathf.Clamp(pitch - delta.y * 0.15f, -60f, 75f);
                }
            }
            if (touches.Count == 0 && pressActive && !held) up = true;
            if (up && pressActive)
            {
                if (!dragging && !pressOverUi) TrySelect(pos);
                pressActive = false; dragging = false;
            }
        }

        void EnterFree()
        {
            if (Mode == CameraMode.Free) return;
            var e = transform.eulerAngles; yaw = e.y; pitch = e.x > 180 ? e.x - 360 : e.x;
            Mode = CameraMode.Free;
            if (ModeChanged != null) ModeChanged(Mode);
        }

        void FreeMove(float dt)
        {
            Vector3 move = Vector3.zero;
            if (InputHelper.KeyHeld(GameKey.W) || InputHelper.KeyHeld(GameKey.Up)) move += transform.forward;
            if (InputHelper.KeyHeld(GameKey.S) || InputHelper.KeyHeld(GameKey.Down)) move -= transform.forward;
            if (InputHelper.KeyHeld(GameKey.D) || InputHelper.KeyHeld(GameKey.Right)) move += transform.right;
            if (InputHelper.KeyHeld(GameKey.A) || InputHelper.KeyHeld(GameKey.Left)) move -= transform.right;
            if (InputHelper.KeyHeld(GameKey.E)) move += Vector3.up;
            if (InputHelper.KeyHeld(GameKey.Q)) move -= Vector3.up;
            float speed = InputHelper.KeyHeld(GameKey.Shift) ? 9f : 3.5f;
            transform.position += move * speed * dt;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 55f, dt * 3f);
            ClampPosition();
        }

        void ClampPosition()
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -ConcertHallBuilder.HallHalfWidth + 0.5f, ConcertHallBuilder.HallHalfWidth - 0.5f);
            p.y = Mathf.Clamp(p.y, 0.6f, ConcertHallBuilder.HallHeight - 0.8f);
            p.z = Mathf.Clamp(p.z, ConcertHallBuilder.HallFrontZ + 0.5f, ConcertHallBuilder.HallBackZ - 1f);
            transform.position = p;
        }

        void TrySelect(Vector2 screenPos)
        {
            RaycastHit hit;
            if (Physics.Raycast(cam.ScreenPointToRay(screenPos), out hit, 80f))
            {
                var view = hit.collider.GetComponentInParent<MusicianView>();
                if (view != null && MusicianTapped != null) MusicianTapped(view);
            }
        }
    }
}
