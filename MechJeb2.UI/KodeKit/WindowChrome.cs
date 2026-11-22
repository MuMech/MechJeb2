using System;
using System.Collections.Generic;
using KodeKit.Headless;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Drag a target window by this handle (the title bar). The window keeps its place relative to the press point, 1:1 in
    /// screen space; the window's <see cref="WindowFrameModel"/> clamps and snaps it. Only the left button drags: a right drag over a
    /// window is KSP's camera (rule 57), and a right click opens a context menu (<see cref="ContextMenuTrigger"/>).
    /// </summary>
    public class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public WindowBehaviour Target;

        /// <summary>When set and false, this handle doesn't drag (e.g. a body handle while only the title bar may drag).</summary>
        public Func<bool> Enabled;

        bool Active => Target.Draggable && (Enabled == null || Enabled());

        static bool Left(PointerEventData e) => e.button == PointerEventData.InputButton.Left;

        public void OnBeginDrag(PointerEventData e)
        {
            if (!Left(e)) return;
            Target.BringToFront();
            if (Active) Target.BeginMove(e.pressPosition, e.pressEventCamera);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Left(e) || !Active) return;
            // the handle turned on during the drag: the move starts from this event's previous point
            if (!Target.Frame.Dragging) Target.BeginMove(e.position - e.delta, e.pressEventCamera);
            Target.MoveToScreen(e.position, e.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!Left(e)) return;
            Target.EndMove();
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
        public float MinVisible
        {
            get => Frame.MinVisible;
            set => Frame.MinVisible = value;
        }

        /// <summary>
        /// Canvas units within which a dragged window's edge snaps to a screen edge or to another window's edge, for every
        /// window (0: no snapping). The host sets it from its settings.
        /// </summary>
        public static float SnapDistance = 8;

        /// <summary>Where the window is and how a drag moves it: the clamp, the scale, the snapping and the saved position.</summary>
        public WindowFrameModel Frame { get; } = new WindowFrameModel();

        // the shown windows, the ones a drag can snap to
        static readonly List<WindowBehaviour> shown = new List<WindowBehaviour>();

        /// <summary>The windows enabled now (a host that lists the windows on screen: the agent bridge).</summary>
        public static IReadOnlyList<WindowBehaviour> Shown => shown;

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

        /// <summary>
        /// A styled part (a rule-table look), repainted with the window's theme in the restyles' order: after the restyle of the widget
        /// it was put on (its skin look), before the restyles added after it (a tone text over a button's label).
        /// </summary>
        public void AddPart(StyledPart p)
        {
            restyles.Add(p.Restyle);
            PartCount++;
        }

        /// <summary>How many styled parts the window has registered.</summary>
        public int PartCount { get; private set; }

        /// <summary>A theme for this window only (null: Theme.Current), e.g. a custom window's own Compact setting.</summary>
        public Func<Theme> ThemeOverride;

        /// <summary>The theme this window's widgets are styled with.</summary>
        public Theme Theme => ThemeOverride?.Invoke() ?? Theme.Current;
        public void AddRelocalize(Action a) => relocalizes.Add(a);

        /// <summary>The window this behaviour draws (a host that finds windows by their objects: the agent bridge).</summary>
        public KodeWindow Window { get; internal set; }

        /// <summary>
        /// The '#key' each label of the window was set from (<see cref="KodeWindow.SetText"/>): a control built after a label takes
        /// it as its own label's key (<see cref="KodeWindow.LabelBefore"/>), so its label follows a language switch and finds its tooltip.
        /// </summary>
        internal readonly Dictionary<TMPro.TMP_Text, string> TextKeys = new Dictionary<TMPro.TMP_Text, string>();

        public int RestyleCount => restyles.Count;
        public int RelocalizeCount => relocalizes.Count;

        /// <summary>
        /// A window was shown or hidden (its behaviour enabled or disabled: opened, closed, its scene torn down), for an observer of the
        /// whole UI (the agent bridge's change feed). Only windows of a <see cref="KodeWindow"/>.
        /// </summary>
        public static event Action<WindowBehaviour, bool> AnyShown;

        internal void ReportShown()
        {
            if (Window != null && isActiveAndEnabled) AnyShown?.Invoke(this, true);
        }

        void OnEnable()
        {
            shown.Add(this);
            if (Window != null) AnyShown?.Invoke(this, true);
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
        public Vector2 GetImguiPosition(float mjScale)
        {
            Vector2 s = ScreenTopLeft;
            WindowFrameModel.ToImgui(s.x, s.y, mjScale, out float x, out float y);
            return new Vector2(x, y);
        }

        public void SetImguiPosition(Vector2 pos, float mjScale)
        {
            WindowFrameModel.FromImgui(pos.x, pos.y, mjScale, out float x, out float y);
            ScreenTopLeft = new Vector2(x, y);
        }

        /// <summary>
        /// Moves the window by a pointer's move between two screen points: snapped and clamped during a drag
        /// (<see cref="BeginMove"/>), only clamped otherwise.
        /// </summary>
        public void MoveByScreenDelta(Vector2 fromScreen, Vector2 toScreen, Camera cam)
        {
            var parent = (RectTransform)Rect.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, fromScreen, cam, out Vector2 a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, toScreen, cam, out Vector2 b);
            LoadFrame();
            Frame.DragBy(b.x - a.x, b.y - a.y);
            StoreFrame();
        }

        /// <summary>During a drag (<see cref="BeginMove(Vector2, Camera)"/>), the pointer is at a screen point.</summary>
        public void MoveToScreen(Vector2 screen, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent, screen, cam, out Vector2 p);
            LoadFrame();
            Frame.DragTo(p.x, p.y);
            StoreFrame();
        }

        /// <summary>
        /// Starts a drag with the button pressed at a screen point: the window keeps its place relative to it, and the windows
        /// shown on the same canvas are what it snaps to (<see cref="SnapDistance"/>), until <see cref="EndMove"/>.
        /// </summary>
        public void BeginMove(Vector2 pressScreen, Camera cam)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent, pressScreen, cam, out Vector2 p);
            BeginMove(p.x, p.y);
        }

        /// <summary>Starts a drag moved by deltas (<see cref="MoveByScreenDelta"/>).</summary>
        public void BeginMove() => BeginMove(0, 0);

        void BeginMove(float pressX, float pressY)
        {
            LoadFrame();
            Frame.SnapDistance = SnapDistance;
            Frame.ClearOthers();
            if (SnapDistance > 0)
            {
                Transform parent = Rect.parent;
                for (int i = 0; i < shown.Count; i++)
                {
                    WindowBehaviour w = shown[i];
                    if (w == this || w.Rect.parent != parent) continue;
                    Frame.AddOther(w.CanvasBox);
                }
            }

            Frame.BeginDrag(pressX, pressY);
            StoreFrame();
        }

        public void EndMove() => Frame.EndDrag();

        public void NotifyMoved() => Moved?.Invoke(this);

        // the parent's and the window's sizes at the last clamp
        Vector2 clampedParent, clampedSize;

        // the model takes the window's place: its top-left corner (pivot 0,1) in the parent's units, its size, its scale
        void LoadFrame()
        {
            Rect p = ((RectTransform)Rect.parent).rect;
            Vector3 pos = Rect.localPosition;
            Rect r = Rect.rect;
            Frame.Load(pos.x, pos.y, r.width, r.height, Scale, new FrameBox(p.xMin, p.yMax, p.xMax, p.yMin));
            clampedParent = p.size;
            clampedSize = r.size;
        }

        // the window's box in its parent's units (y up), read without touching its frame or its clamp state
        FrameBox CanvasBox
        {
            get
            {
                Vector3 pos = Rect.localPosition;
                Vector2 size = Rect.rect.size * Scale;
                return new FrameBox(pos.x, pos.y, pos.x + size.x, pos.y - size.y);
            }
        }

        void StoreFrame()
        {
            Vector3 pos = Rect.localPosition;
            if (pos.x == Frame.X && pos.y == Frame.Y) return;
            Rect.localPosition = new Vector3(Frame.X, Frame.Y, pos.z);
        }

        /// <summary>Keep at least MinVisible units of the window inside its parent (the canvas).</summary>
        public void Clamp()
        {
            LoadFrame();
            Frame.Clamp();
            StoreFrame();
        }

        // ---- z-order ----

        /// <summary>
        /// Above the other windows, and below the stock dialogs that share the canvas: the PopupDialogs (the pause menu is
        /// one), and the dialogs KSP parents there without a PopupDialog (science results, crew hatch, vessel rename, craft
        /// browser, robotic controller, part category popup).
        /// </summary>
        public void BringToFront()
        {
            Transform canvas = Rect.parent;
            int top = canvas.childCount - 1;
            for (int i = top; i >= 0; i--)
            {
                Transform child = canvas.GetChild(i);
                if (child == Rect) continue;
                if (!IsStockDialog(child)) break;
                top--;
            }
            Rect.SetSiblingIndex(top);
        }

        static bool IsStockDialog(Transform t) =>
            t.GetComponent<PopupDialog>() != null
            || t.GetComponent<KSP.UI.Screens.Flight.Dialogs.ExperimentsResultDialog>() != null
            || t.GetComponent<KSP.UI.Screens.Flight.Dialogs.CrewHatchDialog>() != null
            || t.GetComponent<KSP.UI.Screens.VesselRenameDialog>() != null
            || t.GetComponent<KSP.UI.Screens.CraftBrowserDialog>() != null
            || t.GetComponent<Expansions.Serenity.RoboticControllerWindow>() != null
            || t.GetComponent<KSP.UI.Screens.PartCategorizerPopup>() != null;

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

        // the clicks that would go through to the scene, not the keyboard's flight controls; a focused EditField locks the keyboard
        const ControlTypes HoverLock = ControlTypes.ALLBUTCAMERAS & ~ControlTypes.KEYBOARDINPUT;

        void Lock()
        {
            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch?.Lock(true, true, true, lockId);
            else if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium)
                InputLockManager.SetControlLock(HoverLock, lockId);
            Locked = true;
        }

        void Unlock()
        {
            // what EditorLogic.Unlock does, also when the editor is already gone
            InputLockManager.RemoveControlLock(lockId);
            Locked = false;
        }

        void OnDisable()
        {
            shown.Remove(this);
            if (Window != null) AnyShown?.Invoke(this, false);
            Frame.EndDrag();
            Theme.Changed -= Restyle;
            GameEvents.onLanguageSwitched.Remove(Relocalize);
            Hovered = false;
            if (Locked) Unlock();
        }
    }
}
