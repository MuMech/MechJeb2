using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>Drag a target window by this handle (the title bar). Pointer delta is applied 1:1 in screen space.</summary>
    public class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public WindowBehaviour Target;

        /// <summary>When set and false, this handle doesn't drag (e.g. a body handle while only the title bar may drag).</summary>
        public Func<bool> Enabled;

        bool Active => Target.Draggable && (Enabled == null || Enabled());

        public void OnBeginDrag(PointerEventData e) => Target.BringToFront();

        public void OnDrag(PointerEventData e)
        {
            if (Active) Target.MoveByScreenDelta(e.position - e.delta, e.position, e.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (Active) Target.NotifyMoved();
        }
    }

    /// <summary>Clicking a toggle's label flips the toggle, like GUILayout.Toggle.</summary>
    public class ToggleLabelClick : MonoBehaviour, IPointerClickHandler
    {
        public Toggle Toggle;

        public void OnPointerClick(PointerEventData e)
        {
            if (Toggle.interactable) Toggle.isOn = !Toggle.isOn;
        }
    }

    /// <summary>
    /// Window-level behaviour that IMGUI's MechJeb windows get from DisplayModule and MechJebCore:
    /// clamping on screen, bring-to-front, click-through locks, MechJeb UI scale and positions in IMGUI coordinates.
    /// </summary>
    public class WindowBehaviour : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// When false, windows don't take click-through locks themselves; the host reads Hovered and locks once
        /// for all its windows (MechJebCore.PreventClickthrough via IMechJebUiHost.MouseOverWindow).
        /// </summary>
        public static bool ManageLocks = true;

        /// <summary>Pixels (canvas units) of the window that must stay on screen, like DisplayModule's clamp.</summary>
        public float MinVisible = 20;

        /// <summary>Called after the user moved the window (end of drag), so the owner can persist it.</summary>
        public Action<WindowBehaviour> Moved;

        /// <summary>False keeps the window where it is (DisplayModule.WindowLocked).</summary>
        public bool Draggable = true;

        public RectTransform Rect { get; private set; }
        public bool Hovered { get; private set; }
        public bool Locked { get; private set; }

        string lockId;

        void Awake()
        {
            Rect = (RectTransform)transform;
            lockId = "KodeKit_" + GetInstanceID();
        }

        // ---- theme and language ----

        readonly List<Action> restyles = new List<Action>();
        readonly List<Action> relocalizes = new List<Action>();
        int styledVersion = -1;
        string localizedLanguage;

        public void AddRestyle(Action a) => restyles.Add(a);

        /// <summary>A theme for this window only (null: Theme.Current), e.g. a custom window's own Compact setting.</summary>
        public Func<Theme> ThemeOverride;

        /// <summary>The theme this window's widgets are styled with.</summary>
        public Theme Theme => ThemeOverride?.Invoke() ?? Theme.Current;
        public void AddRelocalize(Action a) => relocalizes.Add(a);

        public int RestyleCount => restyles.Count;
        public int RelocalizeCount => relocalizes.Count;

        void OnEnable()
        {
            Theme.Changed += Restyle;
            GameEvents.onLanguageSwitched.Add(Relocalize);
            // catch up with changes made while hidden
            if (styledVersion >= 0 && styledVersion != Theme.Version) Restyle();
            if (localizedLanguage != null && localizedLanguage != KSP.Localization.Localizer.CurrentLanguage) Relocalize();
            styledVersion = Theme.Version;
            localizedLanguage = KSP.Localization.Localizer.CurrentLanguage;
        }

        public void Restyle()
        {
            for (int i = 0; i < restyles.Count; i++) restyles[i]();
            styledVersion = Theme.Version;
        }

        void Relocalize()
        {
            for (int i = 0; i < relocalizes.Count; i++) relocalizes[i]();
            localizedLanguage = KSP.Localization.Localizer.CurrentLanguage;
        }

        // ---- scale ----

        /// <summary>MechJeb's own UI scale (GuiUtils.Scale). KSP's global UI scale already applies through the canvas.</summary>
        public float Scale
        {
            get => Rect.localScale.x;
            set
            {
                Rect.localScale = new Vector3(value, value, 1);
                Clamp();
            }
        }

        // ---- positions ----

        Camera Cam
        {
            get
            {
                Canvas c = GetComponentInParent<Canvas>().rootCanvas;
                return c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
            }
        }

        /// <summary>Top-left corner in screen pixels, origin at the top-left of the screen (IMGUI convention).</summary>
        public Vector2 ScreenTopLeft
        {
            get
            {
                var corners = new Vector3[4];
                Rect.GetWorldCorners(corners); // 1 = top-left
                Vector2 s = RectTransformUtility.WorldToScreenPoint(Cam, corners[1]);
                return new Vector2(s.x, Screen.height - s.y);
            }
            set
            {
                var parent = (RectTransform)Rect.parent;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(value.x, Screen.height - value.y), Cam, out Vector2 local);
                // pivot is top-left, so the local position is the top-left corner
                Rect.localPosition = new Vector3(local.x, local.y, Rect.localPosition.z);
                Clamp();
            }
        }

        /// <summary>
        /// Position in MechJeb's IMGUI units (screen pixels / GuiUtils.Scale), so DisplayModule.WindowVector keeps working.
        /// </summary>
        public Vector2 GetImguiPosition(float mjScale) => ScreenTopLeft / mjScale;

        public void SetImguiPosition(Vector2 pos, float mjScale) => ScreenTopLeft = pos * mjScale;

        public void MoveByScreenDelta(Vector2 fromScreen, Vector2 toScreen, Camera cam)
        {
            var parent = (RectTransform)Rect.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, fromScreen, cam, out Vector2 a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, toScreen, cam, out Vector2 b);
            Rect.localPosition += (Vector3)(b - a);
            Clamp();
        }

        public void NotifyMoved() => Moved?.Invoke(this);

        // the parent's and the window's sizes at the last clamp
        Vector2 clampedParent, clampedSize;

        /// <summary>Keep at least MinVisible units of the window inside its parent (the canvas).</summary>
        public void Clamp()
        {
            var parent = (RectTransform)Rect.parent;
            Rect p = parent.rect;
            clampedParent = p.size;
            clampedSize = Rect.rect.size;
            Vector2 size = clampedSize * Scale;
            Vector3 pos = Rect.localPosition; // top-left corner (pivot 0,1)

            float minX = p.xMin - size.x + MinVisible;
            float maxX = p.xMax - MinVisible;
            float minY = p.yMin + MinVisible;              // top edge can't go below bottom + MinVisible
            float maxY = p.yMax;                           // title bar never above the top of the screen
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            Rect.localPosition = pos;
        }

        // ---- z-order ----

        public void BringToFront() => Rect.SetAsLastSibling();

        static readonly List<RaycastResult> hits = new List<RaycastResult>();

        /// <summary>Bring the window under a screen point to the front (the topmost raycast hit decides).</summary>
        public static WindowBehaviour BringToFrontAt(Vector2 screen)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return null;
            hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screen }, hits);
            if (hits.Count == 0) return null;
            var w = hits[0].gameObject.GetComponentInParent<WindowBehaviour>();
            if (w != null) w.BringToFront();
            return w;
        }

        static int lastFocusFrame = -1;

        void Update()
        {
            // Buttons and fields consume pointer-down, so focus is handled here once per click for all windows.
            if (lastFocusFrame != Time.frameCount && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            {
                lastFocusFrame = Time.frameCount;
                BringToFrontAt(Input.mousePosition);
            }

            // the resolution or KSP's UI scale (the canvas size) changed, or the window grew: keep it on screen, as IMGUI's
            // DisplayModule.WindowPos does every frame (the owner's stored position only changes when the player drags it)
            if (((RectTransform)Rect.parent).rect.size != clampedParent || Rect.rect.size != clampedSize) Clamp();

            // Same policy as MechJebCore.PreventClickthrough: lock while hovered, but not when a right-drag
            // (camera rotation) starts over the window. Off when the host application owns the lock (MechJeb).
            if (!ManageLocks)
            {
                if (Locked) Unlock();
                return;
            }
            if (!Locked && Hovered && !Input.GetMouseButton(1)) Lock();
            else if (Locked && !Hovered) Unlock();
        }

        // ---- click-through locks ----

        public void OnPointerEnter(PointerEventData e) => Hovered = true;

        public void OnPointerExit(PointerEventData e) => Hovered = false;

        /// <summary>For tests and for code that knows the pointer state (e.g. after showing a window under the mouse).</summary>
        public void SetHovered(bool hovered) => Hovered = hovered;

        public string LockId => lockId;

        void Lock()
        {
            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch?.Lock(true, true, true, lockId);
            else if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium)
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, lockId);
            Locked = true;
        }

        void Unlock()
        {
            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch?.Unlock(lockId);
            else
                InputLockManager.RemoveControlLock(lockId);
            Locked = false;
        }

        void OnDisable()
        {
            Theme.Changed -= Restyle;
            GameEvents.onLanguageSwitched.Remove(Relocalize);
            Hovered = false;
            if (Locked) Unlock();
        }
    }
}
