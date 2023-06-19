using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class DisplayModule : ComputerModule
    {
        public bool hidden;

        public Rect windowPos
        {
            get
            {
                if (!HighLogic.LoadedSceneIsEditor)
                    return new Rect(windowVector.x, windowVector.y, windowVector.z, windowVector.w);
                return new Rect(windowVectorEditor.x, windowVectorEditor.y, windowVectorEditor.z, windowVectorEditor.w);
            }
            set
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
                    if (windowVector != newPos)
                    {
                        Dirty        = true;
                        windowVector = newPos;
                    }
                }
                else
                {
                    if (windowVectorEditor != newPos)
                    {
                        Dirty              = true;
                        windowVectorEditor = newPos;
                    }
                }
            }
        }

        // Those field should be private but Persistent has a bug that prevent it to work properly on parent class private fields

        [Persistent(pass = (int)Pass.GLOBAL)]
        public Vector4 windowVector = new Vector4(10, 40, 0, 0); //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.GLOBAL)]
        public Vector4
            windowVectorEditor = new Vector4(10, 40, 0, 0); //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.GLOBAL)]
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

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool showInEditor;

        public bool ShowInEditor
        {
            get => showInEditor;
            set
            {
                if (showInEditor != value)
                {
                    showInEditor = value;
                    Dirty        = true;
                }
            }
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool isOverlay;

        public bool IsOverlay
        {
            get => isOverlay;
            set
            {
                if (isOverlay != value)
                {
                    isOverlay = value;
                    Dirty     = true;
                }
            }
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool locked;

        public bool Locked
        {
            get => locked;
            set
            {
                if (locked != value)
                {
                    locked = value;
                    Dirty  = true;
                }
            }
        }

        internal bool enabledEditor;
        internal bool enabledFlight;

        private GUILayoutOption[] windowOptions;

        public bool showInCurrentScene => HighLogic.LoadedSceneIsEditor ? showInEditor : showInFlight;

        public        int ID;
        public static int nextID = 72190852;

        public DisplayModule(MechJebCore core)
            : base(core)
        {
            ID = nextID;
            nextID++;
        }

        public virtual GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[0];
        }

        protected void WindowGUI(int windowID, bool draggable)
        {
            if (!isOverlay && GUI.Button(new Rect(windowPos.width - 18, 2, 16, 16), ""))
            {
                Enabled = false;
            }

//            if (GUI.Button(new Rect(windowPos.width - 40, 2, 20, 16), (locked ? "X" : "O"))) // lock button needs an icon, letters look crap
//            {
//                locked = !locked;
//            }

            bool allowDrag = !locked;
            if (!locked && !isOverlay && Core.Settings.useTitlebarDragging)
            {
                float x = Mouse.screenPos.x / GuiUtils.scale;
                float y = Mouse.screenPos.y / GuiUtils.scale;
                allowDrag = x >= windowPos.xMin + 3 && x <= windowPos.xMin + windowPos.width - 3 &&
                            y >= windowPos.yMin + 3 && y <= windowPos.yMin + 17;
            }

            if (draggable && allowDrag)
                GUI.DragWindow();
        }

        protected void ProfiledWindowGUI(int windowID)
        {
            Profiler.BeginSample(GetType().Name);
            WindowGUI(windowID);
            Profiler.EndSample();
        }

        protected virtual void WindowGUI(int windowID)
        {
            WindowGUI(windowID, true);
        }

        public virtual void DrawGUI(bool inEditor)
        {
            if (showInCurrentScene)
            {
                // Cache the array to not create one each frame
                if (windowOptions == null)
                    windowOptions = WindowOptions();

                windowPos = GUILayout.Window(ID, windowPos, ProfiledWindowGUI, isOverlay ? "" : GetName(), windowOptions);

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
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);

            if (global != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                    enabledEditor = Enabled;
                if (HighLogic.LoadedSceneIsFlight)
                    enabledFlight = Enabled;

                global.AddValue("enabledEditor", enabledEditor);
                global.AddValue("enabledFlight", enabledFlight);
            }
//            if (global != null) global.AddValue("locked", locked);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            bool useOldConfig = true;
            if (global != null && global.HasValue("enabledEditor"))
            {
                bool loadedEnabled;
                if (bool.TryParse(global.GetValue("enabledEditor"), out loadedEnabled))
                {
                    enabledEditor = loadedEnabled;
                    useOldConfig  = false;
                    if (HighLogic.LoadedSceneIsEditor)
                        Enabled = loadedEnabled;
                }
            }

            if (global != null && global.HasValue("enabledFlight"))
            {
                bool loadedEnabled;
                if (bool.TryParse(global.GetValue("enabledFlight"), out loadedEnabled))
                {
                    enabledFlight = loadedEnabled;
                    useOldConfig  = false;
                    if (HighLogic.LoadedSceneIsFlight)
                        Enabled = loadedEnabled;
                }
            }

            if (useOldConfig)
            {
                if (global != null && global.HasValue("enabled"))
                {
                    bool loadedEnabled;
                    if (bool.TryParse(global.GetValue("enabled"), out loadedEnabled))
                    {
                        Enabled = loadedEnabled;
                    }
                }

                enabledEditor = Enabled;
                enabledFlight = Enabled;
            }

            //            if (global != null && global.HasValue("locked"))
            //            {
            //                bool loadedLocked;
            //                if (bool.TryParse(global.GetValue("locked"), out loadedLocked)) locked = loadedLocked;
            //            }
        }

        private ComputerModule[] makesActive;

        public virtual bool isActive()
        {
            if (makesActive == null)
                makesActive = new ComputerModule[] { Core.Attitude, Core.Thrust, Core.Rover, Core.Node, Core.RCS, Core.Rcsbal };

            bool active = false;
            for (int i = 0; i < makesActive.Length; i++)
            {
                ComputerModule m = makesActive[i];
                if (m != null)
                {
                    if (active |= m.Users.RecursiveUser(this))
                        break;
                }
            }

            return active;
        }

        public override void UnlockCheck()
        {
            if (!UnlockChecked)
            {
                bool prevEn = Enabled;
                Enabled = true;
                base.UnlockCheck();
                if (UnlockParts.Trim().Length > 0 || UnlockTechs.Trim().Length > 0 || !IsSpaceCenterUpgradeUnlocked())
                {
                    hidden = !Enabled;
                    if (hidden)
                    {
                        prevEn = false;
                    }
                }

                Enabled = prevEn;
            }
        }

        public virtual string GetName()
        {
            return "Display Module";
        }

        public virtual string IconName()
        {
            return "Display Module Icon";
        }
    }
}
