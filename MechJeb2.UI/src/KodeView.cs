using KodeKit;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// A KodeKit window for one DisplayModule. The module stays the source of truth for open/closed (Enabled),
    /// position (WindowVector in flight, WindowVectorEditor in the editor, in IMGUI units / GuiUtils.Scale),
    /// window lock and persistence; the view only draws it.
    /// </summary>
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

    public abstract class KodeView
    {
        public readonly DisplayModule Module;
        public KodeWindow Window { get; private set; }
        public bool Visible => Window != null && Window.Root.gameObject.activeSelf;

        /// <summary>Set when the window's structure must change; the host destroys and rebuilds the view.</summary>
        public bool RebuildRequested { get; protected set; }

        protected KodeView(DisplayModule module) => Module = module;

        protected abstract float MinWidth { get; }

        /// <summary>Add the window's contents (called once).</summary>
        protected abstract void BuildContents(KodeWindow w);

        /// <summary>Per-frame model work the IMGUI WindowGUI did before drawing (runs before the bindings).</summary>
        protected virtual void BeforeSync() { }

        /// <summary>The last Build's cost in ms: creating the window's contents, and the canvas update that lays it out (for tests).</summary>
        public static double LastContentsMs, LastCanvasMs;

        public void Build(RectTransform canvas)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Window = new KodeWindow(canvas, Module.GetName(), MinWidth, Vector2.zero);
            Window.OnClose = () => Module.Enabled = false;
            BuildContents(Window);
            builtOverlay = Module.IsOverlay;
            if (builtOverlay) MakeOverlay();
            LastContentsMs = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            Canvas.ForceUpdateCanvases();
            LastCanvasMs = timer.Elapsed.TotalMilliseconds;
            ApplyScale();
            Vector4 v = StoredPosition;
            Window.Behaviour.SetImguiPosition(new Vector2(v.x, v.y), GuiUtils.Scale);
            Window.Behaviour.Moved = _ => StorePosition();
        }

        public void ApplyScale()
        {
            if (Window != null) Window.Behaviour.Scale = GuiUtils.Scale;
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

        public void SetVisible(bool visible)
        {
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

        bool beforeSyncFailed;

        public void Sync()
        {
            if (Module.IsOverlay != builtOverlay) RebuildRequested = true;
            Window.Behaviour.Draggable = !Module.WindowLocked;
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

        public void Destroy()
        {
            // on a scene change the window can already be gone when the host is destroyed: Unity's null check covers that
            if (Window != null && Window.Root != null) Object.Destroy(Window.Root.gameObject);
            Window = null;
        }
    }
}
