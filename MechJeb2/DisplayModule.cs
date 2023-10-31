using System;
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
                ? new Rect(_windowVector.x, _windowVector.y, _windowVector.z, _windowVector.w)
                : new Rect(_windowVectorEditor.x, _windowVectorEditor.y, _windowVectorEditor.z, _windowVectorEditor.w);

            protected set
            {
                var newPos = new Vector4(
                    Math.Min(Math.Max(value.x, 0), GuiUtils.scaledScreenWidth - value.width),
                    Math.Min(Math.Max(value.y, 0), GuiUtils.scaledScreenHeight - value.height),
                    value.width, value.height
                );
                newPos.x = Mathf.Clamp(newPos.x, 10 - value.width, GuiUtils.scaledScreenWidth - 10);
                newPos.y = Mathf.Clamp(newPos.y, 10 - value.height, GuiUtils.scaledScreenHeight - 10);

                if (!HighLogic.LoadedSceneIsEditor)
                {
                    if (_windowVector != newPos)
                    {
                        Dirty         = true;
                        _windowVector = newPos;
                    }
                }
                else
                {
                    if (_windowVectorEditor != newPos)
                    {
                        Dirty               = true;
                        _windowVectorEditor = newPos;
                    }
                }
            }
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public Vector4 _windowVector = new Vector4(10, 40, 0, 0); //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.GLOBAL)]
        public Vector4
            _windowVectorEditor = new Vector4(10, 40, 0, 0); //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool _showInFlight = true;

        public bool ShowInFlight
        {
            get => _showInFlight;
            set
            {
                if (_showInFlight != value)
                {
                    _showInFlight = value;
                    Dirty         = true;
                }
            }
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool _showInEditor;

        public bool ShowInEditor
        {
            get => _showInEditor;
            set
            {
                if (_showInEditor == value) return;
                _showInEditor = value;
                Dirty         = true;
            }
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool _isOverlay;

        public bool IsOverlay
        {
            get => _isOverlay;
            set
            {
                if (_isOverlay == value) return;
                _isOverlay = value;
                Dirty      = true;
            }
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool _locked;

        public bool Locked
        {
            get => _locked;
            set
            {
                if (_locked == value) return;
                _locked = value;
                Dirty   = true;
            }
        }

        internal bool EnabledEditor;
        internal bool EnabledFlight;

        private GUILayoutOption[] _windowOptions;

        public bool ShowInCurrentScene => HighLogic.LoadedSceneIsEditor ? _showInEditor : _showInFlight;

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
            if (!_isOverlay && GUI.Button(new Rect(WindowPos.width - 18, 2, 16, 16), ""))
            {
                Enabled = false;
            }

//            if (GUI.Button(new Rect(windowPos.width - 40, 2, 20, 16), (locked ? "X" : "O"))) // lock button needs an icon, letters look crap
//            {
//                locked = !locked;
//            }

            bool allowDrag = !_locked;
            if (!_locked && !_isOverlay && Core.Settings.useTitlebarDragging)
            {
                float x = Mouse.screenPos.x / GuiUtils.scale;
                float y = Mouse.screenPos.y / GuiUtils.scale;
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

            WindowPos = GUILayout.Window(_id, WindowPos, ProfiledWindowGUI, _isOverlay ? "" : GetName(), _windowOptions);

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
