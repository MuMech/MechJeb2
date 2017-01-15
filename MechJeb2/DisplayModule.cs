using System;
using UnityEngine;

namespace MuMech
{
    public class DisplayModule : ComputerModule
    {
        public bool hidden = false;

        public Rect windowPos
        {
            get
            {
                if (!HighLogic.LoadedSceneIsEditor)                
                    return new Rect(windowVector.x, windowVector.y, windowVector.z, windowVector.w);                
                else
                    return new Rect(windowVectorEditor.x, windowVectorEditor.y, windowVectorEditor.z, windowVectorEditor.w);
            }
            set
            {
                Vector4 newPos = new Vector4(
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
                        dirty = true;
                        windowVector = newPos;
                    }
                }
                else
                {
                    if (windowVectorEditor != newPos)
                    {
                        dirty = true;
                        windowVectorEditor = newPos;
                    }
                }
            }
        }


        // Those field should be private but Persistant has a bug that prevent it to work properly on parent class private fields

        [Persistent(pass = (int)Pass.Global)]
        public Vector4 windowVector = new Vector4(10, 40, 0, 0); //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.Global)]
        public Vector4 windowVectorEditor = new Vector4(10, 40, 0, 0); //Persistence is via a Vector4 since ConfigNode doesn't know how to serialize Rects

        [Persistent(pass = (int)Pass.Global)]
        public bool showInFlight = true;
        public bool ShowInFlight
        {
            get { return showInFlight; }
            set
            {
                if (showInFlight != value)
                {
                    showInFlight = value;
                    dirty = true;
                }
            }
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool showInEditor = false;
        public bool ShowInEditor
        {
            get { return showInEditor; }
            set
            {
                if (showInEditor != value)
                {
                    showInEditor = value;
                    dirty = true;
                }
            }
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool isOverlay = false;
        public bool IsOverlay
        {
            get { return isOverlay; }
            set
            {
                if (isOverlay != value)
                {
                    isOverlay = value;
                    dirty = true;
                }
            }
        }

        [Persistent(pass = (int)Pass.Global)]
        public bool locked = false;
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (locked != value)
                {
                    locked = value;
                    dirty = true;
                }
            }
        }

        internal bool enabledEditor;
        internal bool enabledFlight;

        private GUILayoutOption[] windowOptions;

        public bool showInCurrentScene { get { return (HighLogic.LoadedSceneIsEditor ? showInEditor : showInFlight); } }

        public int ID;
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
                enabled = false;
            }

//            if (GUI.Button(new Rect(windowPos.width - 40, 2, 20, 16), (locked ? "X" : "O"))) // lock button needs an icon, letters look crap
//            {
//                locked = !locked;
//            }
            
            bool allowDrag = !locked;
            if (locked && !isOverlay && core.settings.useTitlebarDragging)
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
                    enabledEditor = enabled;
                if (HighLogic.LoadedSceneIsFlight)
                    enabledFlight = enabled;
                
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
                    useOldConfig = false;
                    if (HighLogic.LoadedSceneIsEditor)
                        enabled = loadedEnabled;
                }
            }

            if (global != null && global.HasValue("enabledFlight"))
            {
                bool loadedEnabled;
                if (bool.TryParse(global.GetValue("enabledFlight"), out loadedEnabled))
                {
                    enabledFlight = loadedEnabled;
                    useOldConfig = false;
                    if (HighLogic.LoadedSceneIsFlight)
                        enabled = loadedEnabled;
                }
            }

            if (useOldConfig)
            {
                if (global != null && global.HasValue("enabled"))
                {
                    bool loadedEnabled;
                    if (bool.TryParse(global.GetValue("enabled"), out loadedEnabled))
                    {
                        enabled = loadedEnabled;
                    }
                }
                enabledEditor = enabled;
                enabledFlight = enabled;
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
                makesActive = new ComputerModule[] { core.attitude, core.thrust, core.rover, core.node, core.rcs, core.rcsbal };

            bool active = false;
            for (int i = 0; i < makesActive.Length; i++)
            {
                var m = makesActive[i];
                if (m != null)
                {
                    if (active |= m.users.RecursiveUser(this))
                        break;
                }
            }
            return active;
        }

        public override void UnlockCheck()
        {
            if (!unlockChecked)
            {
                bool prevEn = enabled;
                enabled = true;
                base.UnlockCheck();
                if (unlockParts.Trim().Length > 0 || unlockTechs.Trim().Length > 0 || !IsSpaceCenterUpgradeUnlocked())
                {
                    hidden = !enabled;
                    if (hidden)
                    {
                        prevEn = false;
                    }
                }
                enabled = prevEn;
            }
        }

        public virtual string GetName()
        {
            return "Display Module";
        }
    }
}
