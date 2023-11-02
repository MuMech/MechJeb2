using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class DisplayModule : ComputerModule
    {
        public bool Hidden;

        public Rect WindowPos
        {
            get => !HighLogic.LoadedSceneIsEditor
                ? new Rect(WindowVector.x, WindowVector.y, WindowVector.z, WindowVector.w)
                : new Rect(WindowVectorEditor.x, WindowVectorEditor.y, WindowVectorEditor.z, WindowVectorEditor.w);

            protected set
            {
                var newPos = new Vector4(
                    Math.Min(Math.Max(value.x, 0), GuiUtils.ScaledScreenWidth - value.width),
                    Math.Min(Math.Max(value.y, 0), GuiUtils.ScaledScreenHeight - value.height),
                    value.width, value.height
                );
                newPos.x = Mathf.Clamp(newPos.x, 10 - value.width, GuiUtils.ScaledScreenWidth - 10);
                newPos.y = Mathf.Clamp(newPos.y, 10 - value.height, GuiUtils.ScaledScreenHeight - 10);

                if (!HighLogic.LoadedSceneIsEditor)
                {
                    if (WindowVector != newPos)
                    {
                        Dirty        = true;
                        WindowVector = newPos;
                    }
                }
                else
                {
                    if (WindowVectorEditor != newPos)
                    {
                        Dirty              = true;
                        WindowVectorEditor = newPos;
                    }
                }
            }
        }

        // Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public Vector4 WindowVector = new Vector4(10, 40, 0, 0);

        // Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public Vector4 WindowVectorEditor = new Vector4(10, 40, 0, 0);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        // ReSharper disable once InconsistentNaming
        public bool showInFlight = true;

        public bool ShowInFlight
        {
            get => showInFlight;
            set
            {
                if (showInFlight != value)
                {
                    showInFlight = value;
                    Dirty        = true;
                }
            }
        }

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        // ReSharper disable once InconsistentNaming
        public bool showInEditor;

        public bool ShowInEditor
        {
            get => showInEditor;
            set
            {
                if (showInEditor == value) return;
                showInEditor = value;
                Dirty        = true;
            }
        }

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool IsOverlayConfig;

        public bool IsOverlay
        {
            get => IsOverlayConfig;
            set
            {
                if (IsOverlayConfig == value) return;
                IsOverlayConfig = value;
                Dirty           = true;
            }
        }

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool LockedConfig;

        public bool Locked
        {
            get => LockedConfig;
            set
            {
                if (LockedConfig == value) return;
                LockedConfig = value;
                Dirty        = true;
            }
        }

        internal bool EnabledEditor;
        internal bool EnabledFlight;

        private GUILayoutOption[] _windowOptions;

        public bool ShowInCurrentScene => HighLogic.LoadedSceneIsEditor ? showInEditor : showInFlight;

        private readonly int _id;
        private static   int _nextID = 72190852;

        protected DisplayModule(MechJebCore core)
            : base(core)
        {
            _id = _nextID;
            _nextID++;
        }

        protected virtual GUILayoutOption[] WindowOptions() => Array.Empty<GUILayoutOption>();

        protected void WindowGUI(int windowID, bool draggable)
        {
            if (!IsOverlayConfig && GUI.Button(new Rect(WindowPos.width - 18, 2, 16, 16), ""))
            {
                Enabled = false;
            }

//            if (GUI.Button(new Rect(windowPos.width - 40, 2, 20, 16), (locked ? "X" : "O"))) // lock button needs an icon, letters look crap
//            {
//                locked = !locked;
//            }

            bool allowDrag = !LockedConfig;
            if (!LockedConfig && !IsOverlayConfig && Core.Settings.useTitlebarDragging)
            {
                float x = Mouse.screenPos.x / GuiUtils.Scale;
                float y = Mouse.screenPos.y / GuiUtils.Scale;
                allowDrag = x >= WindowPos.xMin + 3 && x <= WindowPos.xMin + WindowPos.width - 3 &&
                            y >= WindowPos.yMin + 3 && y <= WindowPos.yMin + 17;
            }

            if (draggable && allowDrag)
                GUI.DragWindow();
        }

        private void ProfiledWindowGUI(int windowID)
        {
            Profiler.BeginSample(GetType().Name);
            WindowGUI(windowID);
            Profiler.EndSample();
        }

        protected virtual void WindowGUI(int windowID) => WindowGUI(windowID, true);

        public virtual void DrawGUI(bool inEditor)
        {
            if (!ShowInCurrentScene) return;

            // Cache the array to not create one each frame
            _windowOptions ??= WindowOptions();

            WindowPos = GUILayout.Window(_id, WindowPos, ProfiledWindowGUI, IsOverlayConfig ? "" : GetName(), _windowOptions);

            //                var windows = core.GetComputerModules<DisplayModule>(); // on ice until there's a way to find which window is active, unless you like dragging other windows by snapping
            //
            //                foreach (var w in windows)
            //                {
            //                	if (w == this) { continue; }
            //
            //                	var diffL = (w.windowPos.x + w.windowPos.width - 2) - windowPos.x;
            //                	var diffR = w.windowPos.x - (windowPos.x + windowPos.width + 2);
            //                	var diffT = (w.windowPos.y + w.windowPos.height - 2) - windowPos.y;
            //                	var diffB = w.windowPos.y - (windowPos.y + windowPos.height + 2);
            //
            //                	if (Math.Abs(diffL) <= 8)
            //                	{
            //                		SetPos(w.windowPos.x + w.windowPos.width - 2, windowPos.y);
            //                	}
            //
            //                	if (Math.Abs(diffR) <= 8)
            //                	{
            //                		SetPos(w.windowPos.x - windowPos.width + 2, windowPos.y);
            //                	}
            //                }
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);

            if (global != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                    EnabledEditor = Enabled;
                if (HighLogic.LoadedSceneIsFlight)
                    EnabledFlight = Enabled;

                global.AddValue("enabledEditor", EnabledEditor);
                global.AddValue("enabledFlight", EnabledFlight);
            }
//            if (global != null) global.AddValue("locked", locked);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            bool useOldConfig = true;
            if (global != null && global.HasValue("enabledEditor"))
            {
                if (bool.TryParse(global.GetValue("enabledEditor"), out bool loadedEnabled))
                {
                    EnabledEditor = loadedEnabled;
                    useOldConfig  = false;
                    if (HighLogic.LoadedSceneIsEditor)
                        Enabled = loadedEnabled;
                }
            }

            if (global != null && global.HasValue("enabledFlight"))
            {
                if (bool.TryParse(global.GetValue("enabledFlight"), out bool loadedEnabled))
                {
                    EnabledFlight = loadedEnabled;
                    useOldConfig  = false;
                    if (HighLogic.LoadedSceneIsFlight)
                        Enabled = loadedEnabled;
                }
            }

            if (useOldConfig)
            {
                if (global != null && global.HasValue("enabled"))
                {
                    if (bool.TryParse(global.GetValue("enabled"), out bool loadedEnabled))
                    {
                        Enabled = loadedEnabled;
                    }
                }

                EnabledEditor = Enabled;
                EnabledFlight = Enabled;
            }

            //            if (global != null && global.HasValue("locked"))
            //            {
            //                bool loadedLocked;
            //                if (bool.TryParse(global.GetValue("locked"), out loadedLocked)) locked = loadedLocked;
            //            }
        }

        private ComputerModule[] _makesActive;

        public virtual bool IsActive()
        {
            _makesActive ??= new ComputerModule[] { Core.Attitude, Core.Thrust, Core.Rover, Core.Node, Core.RCS, Core.Rcsbal };

            bool active = false;
            for (int i = 0; i < _makesActive.Length; i++)
            {
                ComputerModule m = _makesActive[i];
                if (m == null) continue;

                if (active |= m.Users.RecursiveUser(this))
                    break;
            }

            return active;
        }

        public override void UnlockCheck()
        {
            if (UnlockChecked) return;

            bool prevEn = Enabled;
            Enabled = true;
            base.UnlockCheck();
            if (UnlockParts.Trim().Length > 0 || UnlockTechs.Trim().Length > 0 || !IsSpaceCenterUpgradeUnlocked())
            {
                Hidden = !Enabled;
                if (Hidden)
                {
                    prevEn = false;
                }
            }

            Enabled = prevEn;
        }

        public virtual string GetName() => "Display Module";

        public virtual string IconName() => "Display Module Icon";
    }
}
