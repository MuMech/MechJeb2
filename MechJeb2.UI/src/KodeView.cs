using KodeKit;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// Marks a KodeView as the new-UI window of a DisplayModule type. The view needs a public constructor
    /// taking that module; KodeHost finds every marked view by reflection.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class KodeViewForAttribute : System.Attribute
    {
        public readonly System.Type Module;
        public KodeViewForAttribute(System.Type module) => Module = module;
    }

    /// <summary>
    /// A KodeKit window for one DisplayModule. The module stays the source of truth for open/closed (Enabled),
    /// position (WindowVector in flight, WindowVectorEditor in the editor, in IMGUI units / GuiUtils.Scale),
    /// window lock and persistence; the view only draws it.
    /// </summary>
    public abstract class KodeView
    {
        public readonly DisplayModule Module;
        public KodeWindow Window { get; private set; }
        public bool Visible => Window != null && Window.Root.gameObject.activeSelf;

        /// <summary>Set when the window's structure must change; the host destroys and rebuilds the view.</summary>
        public bool RebuildRequested { get; protected set; }

        protected KodeView(DisplayModule module) => Module = module;

        protected abstract float MinWidth { get; }

        /// <summary>
        /// The window's name in every language for the agent bridge (<see cref="KodeWindow.ModuleName"/>): the module's type name. A view
        /// whose module type has several windows names each its own way.
        /// </summary>
        protected virtual string AgentName => Module.GetType().Name;

        /// <summary>Add the window's contents (called once).</summary>
        protected abstract void BuildContents(KodeWindow w);

        /// <summary>Per-frame model work of the module (runs before the bindings).</summary>
        protected virtual void BeforeSync() { }

        /// <summary>The last Build's cost in ms: creating the window's contents, and the canvas update that lays it out (for tests).</summary>
        public static double LastContentsMs, LastCanvasMs;

        // timestamps rather than a Stopwatch object: no allocation per build for numbers only tests read
        static double MsSince(long start) => (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

        public void Build(RectTransform canvas)
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            Window = new KodeWindow(canvas, Module.GetName(), MinWidth, Vector2.zero);
            Window.ModuleName = AgentName;
            Window.OnClose = () => Module.Enabled = false;
            Window.Edited = () => Module.Dirty = true;
            BuildContents(Window);
            builtOverlay = Module.IsOverlay;
            if (builtOverlay) MakeOverlay();
            LastContentsMs = MsSince(start);
            start = System.Diagnostics.Stopwatch.GetTimestamp();
            Canvas.ForceUpdateCanvases();
            LastCanvasMs = MsSince(start);
            ApplyScale();
            Vector4 v = StoredPosition;
            Window.Behaviour.SetImguiPosition(new Vector2(v.x, v.y), GuiUtils.Scale);
            Window.Behaviour.Moved = _ => StorePosition();
            Window.Behaviour.BringToFront();
            // rebuilt while one of its fields was being edited: that field keeps the keyboard and the edit
            KodeKit.Headless.FocusOutcome focus = Window.RecoverFocus(Module);
            if (focus != KodeKit.Headless.FocusOutcome.None)
                Debug.Log($"[MechJeb2.UI] the \"{Module.GetName()}\" window was rebuilt during an edit: {focus}");
        }

        public void ApplyScale()
        {
            if (Window != null) Window.Behaviour.Scale = GuiUtils.Scale;
        }

        /// <summary>
        /// A UI scale change, after KodeHost converted the stored positions to the new scale's units: the window grows or
        /// shrinks about its top-left corner and keeps its place on screen. A shown window stores the place it has, which the
        /// screen clamp may have moved; a hidden one is placed from the stored position when it is shown.
        /// </summary>
        public void Rescale()
        {
            if (Window == null) return;
            ApplyScale();
            if (Window.Root.gameObject.activeSelf) StorePosition();
        }

        // DisplayModule.WindowPos keeps one position per scene
        Vector4 StoredPosition
        {
            get => HighLogic.LoadedSceneIsEditor ? Module.WindowVectorEditor : Module.WindowVector;
            set
            {
                if (HighLogic.LoadedSceneIsEditor) Module.WindowVectorEditor = value;
                else Module.WindowVector = value;
            }
        }

        void StorePosition()
        {
            Vector2 p = Window.Behaviour.GetImguiPosition(GuiUtils.Scale);
            Vector4 old = StoredPosition;
            StoredPosition = new Vector4(p.x, p.y, old.z, old.w);
            Module.Dirty = true;
        }

        /// <summary>
        /// Shows or hides the window. <paramref name="opened"/>: the player (or the menu, a shortcut, the palette) opened it, rather
        /// than the UI being shown again (F2, a scene change): it comes above the other windows.
        /// </summary>
        public void SetVisible(bool visible, bool opened = false)
        {
            if (visible && opened) Window.Behaviour.BringToFront();
            if (Window.Root.gameObject.activeSelf == visible) return;
            // shown again: the window may have been moved meanwhile (in IMGUI, or by code), so take the stored position
            if (visible)
            {
                Vector4 v = StoredPosition;
                Window.Behaviour.SetImguiPosition(new Vector2(v.x, v.y), GuiUtils.Scale);
            }
            Window.Root.gameObject.SetActive(visible);
        }

        bool builtOverlay;

        /// <summary>The background of the window in overlay mode (DisplayModule.IsOverlay): transparent unless overridden.</summary>
        protected virtual Color OverlayBackground => new Color(0, 0, 0, 0);

        /// <summary>
        /// Overlay mode, like IMGUI's: no title bar or close button, the background in OverlayBackground, dragged by
        /// any empty part of the window.
        /// </summary>
        void MakeOverlay()
        {
            Window.TitleRow.gameObject.SetActive(false);
            Window.BodyDrag.Enabled = () => true; // IMGUI overlays drag anywhere, whatever the titlebar setting
            UnityEngine.UI.Image bg = Window.Root.BackGround;
            void Paint()
            {
                bg.sprite = null;
                bg.color = OverlayBackground;
            }
            Paint();
            Window.Behaviour.AddRestyle(Paint);
            Window.AddSync(() =>
            {
                if (bg.color != OverlayBackground) Paint(); // the editor's colour picker changes it live
            });
        }

        /// <summary>The window lets the pointer through to what's under it. Only the editor can turn this off again.</summary>
        protected virtual bool ClickThrough => false;

        CanvasGroup clickThroughGroup;

        void ApplyClickThrough()
        {
            if (clickThroughGroup == null)
            {
                if (!ClickThrough) return;
                clickThroughGroup = Window.Root.gameObject.AddComponent<CanvasGroup>();
            }
            clickThroughGroup.blocksRaycasts = !ClickThrough;
        }

        bool beforeSyncFailed;

        public void Sync()
        {
            if (Module.IsOverlay != builtOverlay) RebuildRequested = true;
            Window.Behaviour.Draggable = !Module.WindowLocked;
            ApplyClickThrough();
            // when the view's own per-frame work throws, the window's bindings still run (each isolated by KodeWindow.Sync)
            try
            {
                BeforeSync();
            }
            catch (System.Exception e)
            {
                if (!beforeSyncFailed)
                {
                    beforeSyncFailed = true;
                    Debug.LogError($"[MechJeb2.UI] the update of the \"{Module.GetName()}\" window threw (logged once, its bindings still update):");
                    Debug.LogException(e);
                }
            }
            Window.Sync();
        }

        /// <summary>
        /// Destroys the window. <paramref name="rebuilding"/>: the host builds this module's window again (a rebuild, a language or
        /// layout switch), so a field being edited keeps its edit for the rebuilt window; otherwise (closed, a vessel switch, the
        /// modules recreated) the edit ends with the window, as a focus loss, while its view and module are still there.
        /// </summary>
        public void Destroy(bool rebuilding = false)
        {
            // the undo stack leaves the history now: Object.Destroy is deferred to the end of the frame, and a history undo in
            // between would write through this view's setters
            Window?.Undo?.Detach();
            // a field being edited: its edit waits for the window built again for this module, or ends now
            if (rebuilding) Window?.RememberFocus(Module);
            else Window?.EndFocusedEdit();
            // on a scene change the window can already be gone when the host is destroyed: Unity's null check covers that
            if (Window != null && Window.Root != null) Object.Destroy(Window.Root.gameObject);
            Window = null;
            Destroyed();
        }

        /// <summary>After <see cref="Destroy"/>: a view lets go of what it handed its module.</summary>
        protected virtual void Destroyed() { }
    }
}
